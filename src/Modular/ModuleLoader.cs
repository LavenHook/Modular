using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Modular
{
  public class ModuleLoader<TStartupModule> : Module where TStartupModule : class//where TStartupModule : Module
  {
    //This container is strictly internal becasue of the .net core startup pipeline
    //-The (rough) pipeline sequence is:
    //--Create the host-builder
    //--ConfigureAppConfiguration
    //--Build the host
    //---Create the Host's service provider
    //---Host.CreateStartupClass (using Host's ServiceProvider, which limits ctor DI parameters to IHostEnvironment and IConfiguration)
    //----StartupClass.ConfigureServices
    //---Host.Build ServiceProvider
    //---App.Set service provider
    //----StartupClass.Configure(app, including builde service provider)
    //All that is to say that this serviceContainer is not the same one that is injected at ConfigureServices(...)
    protected ServiceProvider ServiceContainer { get; private set; }

    protected TStartupModule StartupModuleInstance { get; private set; }

    private Dictionary<int, List<Module>> dependencyModules { get; set; }

    public IReadOnlyDictionary<int, Module[]> DependencyModules => dependencyModules.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());

    public override string Name => nameof(ModuleLoader<TStartupModule>);

    public override string Description => $"Registers the startup {typeof(TStartupModule).FullName} services and it's module dependencies";

    public ModuleLoader(IHostEnvironment Environment, IConfiguration Configuration) : base(Environment, Configuration)
    {
      dependencyModules = new Dictionary<int, List<Module>>();
      var builder = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
      builder.AddSingleton(Environment.GetType(), Environment);
      builder.AddSingleton<IHostEnvironment>(p => p.GetService(Environment.GetType()) as IHostEnvironment);
      builder.AddSingleton(Configuration.GetType(), Configuration);
      builder.AddSingleton<IConfiguration>(p => p.GetService(Configuration.GetType()) as IConfiguration);
      foreach (var moduleType in ModuleExtensions.DependsOn<TStartupModule>().Except(new Type[] { typeof(TStartupModule) }))
      {
        builder.AddSingleton(moduleType);
        builder.AddSingleton<Module>(p => p.GetService(moduleType) as Module);
      }
      builder.AddSingleton<TStartupModule>();
      //builder.AddSingleton<Module>(p => p.GetService<TStartupModule>());
      ServiceContainer = builder.BuildServiceProvider();
      //Let this DI container resolve the modules, instead of using reflection
      StartupModuleInstance = ServiceContainer.GetService<TStartupModule>();
      var otherModules = ServiceContainer.GetServices<Module>();
      if (StartupModuleInstance is Module)
      {
        otherModules = otherModules.Except(new Module[] { StartupModuleInstance as Module });
      }
      foreach (var ModuleInstance in otherModules)
      {
        if (!dependencyModules.ContainsKey(ModuleInstance.Priority))
        {
          dependencyModules[ModuleInstance.Priority] = new List<Module>();
        }
        if (ModuleInstance is Module)
        {
          dependencyModules[ModuleInstance.Priority].Add(ModuleInstance);
        }
      }
    }

    public override void ConfigureServices(IServiceCollection services)
    {
      services.AddSingleton<ModuleLoader<TStartupModule>>(this);
      services.AddSingleton<Module>(p => p.GetRequiredService<ModuleLoader<TStartupModule>>() as Module);
      foreach (int priority in DependencyModules.Keys.OrderByDescending(k => k))
      {
        foreach (var ModuleInstance in DependencyModules[priority])
        {
          //services.AddSingleton(ModuleInstance.GetType(), ModuleInstance);
          //services.AddSingleton<Module>(p => p.GetRequiredService(ModuleInstance.GetType()) as Module);
          Type moduleType = ModuleInstance.GetType();
          services.AddSingleton(moduleType, p => ServiceContainer.GetRequiredService(moduleType));
          services.AddSingleton<Module>(p => ServiceContainer.GetRequiredService(moduleType) as Module);
          ModuleInstance.ConfigureServices(services);
        }
      }
      //services.AddSingleton<TStartupModule>(StartupModuleInstance);
      //services.AddSingleton<Module>(p => ServiceContainer.GetRequiredService<TStartupModule>() as Module);
      //StartupModuleInstance.ConfigureServices(services);
    }
  }
}
