using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Modular.AspNetCore.Configuration
{
  /// <summary>
  /// This class is used to inject the required assemblies into the ApplicationRouter so that modularized class
  /// libraries can configure their own UI assembly references
  /// </summary>
  public static class BlazorUIAssemblyExtenstions
  {
    public static IServiceCollection AddBlazorUIAssemblyService(this IServiceCollection services)
    {
      services.TryAddSingleton<BlazorUIAssemblyService>();
      return services;
    }

    public static IServiceCollection AddBlazorUIAssemblyService(this IServiceCollection services, Action<BlazorUIAssemblyServiceOptions> Options)
    {
      return services.Configure(Options).AddBlazorUIAssemblyService();
    }

    public static IServiceCollection AddBlazorUIAssemblies(this IServiceCollection services, IEnumerable<Assembly> Assemblies)
    {
      services.AddBlazorUIAssemblyService(bas => bas.AddRange(Assemblies));
      return services;
    }

    public static IServiceCollection AddBlazorUIAssembly(this IServiceCollection services, System.Reflection.Assembly Assembly)
    {
      services.AddBlazorUIAssemblyService(bas => bas.Add(Assembly));
      return services;
    }

    private class BlazorUIAssemblyCollection
    {
      public IEnumerable<System.Reflection.Assembly> Assemblies { get; set; }
    }
  }
}
