using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Modular.AspNetCore
{
  public static class ModuleHostBuilderExtensions
  {
    public static IWebHostBuilder UseStartupModule<TStartupModule>(this IWebHostBuilder builder) where TStartupModule : class // where TStartupModule : Module
    {
      return builder
        .ConfigureAppConfiguration((b, c) =>
        {
          //TODO: each module's settings should only load the json object that has a name matching the module
          foreach (var moduleType in ModuleExtensions.DependsOn<TStartupModule>()) //TODO: this needs to be ordered ascending by priority
                                                                                   //so that if module_A depends on module_B, then module_B's settings are used, so that module_A cannot highjack module_B's settings
          {
            string conventionalModuleName = $"{Regex.Replace(moduleType.Name, "module$", "", RegexOptions.IgnoreCase)}"; //remove "...Module" if present
            c.AddJsonFile($"{conventionalModuleName}Settings.json", true);
            c.AddJsonFile($"{conventionalModuleName}Settings.{b.HostingEnvironment.EnvironmentName}.json", true);

            var method = moduleType.GetMethod("ConfigureModuleConfiguration", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.IgnoreReturn, null, new Type[] { b.GetType(), c.GetType() }, null);
            if (method is MethodInfo)
            {
              method.Invoke(null, new object[] { b, c });
            }
            else if (method is null)
            {
              method = moduleType.GetMethod("ConfigureModuleConfiguration", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.IgnoreReturn, null, new Type[] { c.GetType() }, null);
              if (method is MethodInfo)
              {
                method.Invoke(null, new object[] { c });
              }
            }
          }
        })
        .ConfigureAppConfiguration((b, c) =>
        {
          //c.AddUserSecrets<TStartupModule>();
          c.AddEnvironmentVariables();
        })
        //SEE: https://github.com/dotnet/aspnetcore/issues/11921#issuecomment-523670519
        //.ConfigureAppConfiguration((b, c) => b.HostingEnvironment.ApplicationName = typeof(TStartupModule).Namespace);
        .ConfigureAppConfiguration((b, c) => b.HostingEnvironment.ApplicationName = typeof(TStartupModule).Assembly.GetName().Name)
        //.UseStartup<EndpointStartupModuleLoader<TStartupModule>>();
        .ConfigureAppConfiguration((b, c) =>
        {
          
          
        })
        .ConfigureServices((c, s) =>
        {
          var ml = new ModuleLoader<TStartupModule>(c.HostingEnvironment, c.Configuration);
          ml.ConfigureServices(s);
          s.AddSingleton(ml);
        })
        .Configure(b =>
        {
          var ml = b.ApplicationServices.GetRequiredService<ModuleLoader<TStartupModule>>();
          foreach (int priority in ml.DependencyModules.Keys.OrderBy(k => k))
          {
            foreach (var endpointModule in ml.DependencyModules[priority].Where(m => m is EndpointModule).Select(m => m as EndpointModule))
            {
              b.Map($"/{endpointModule.EndpointPrefix}", endpoint => endpointModule.ConfigureEndpoint(endpoint));
            }
          }
        })
        .UseStartup<TStartupModule>();
    }
  }
}
