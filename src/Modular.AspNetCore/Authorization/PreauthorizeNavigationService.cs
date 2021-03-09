using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Modular.AspNetCore.Configuration;

namespace Modular.AspNetCore.Authorization
{
  //Parts of this class have been derived from https://stackoverflow.com/a/60383375 and https://stackoverflow.com/a/60164096
  public class PreauthorizeNavigationService
  {
    private ObjectAuthorization ObjectAuthorization { get; set; }

    private PreauthorizeNavigationServiceOptions Options { get; set; }

    public IReadOnlyDictionary<Type, string[]> PageRoutes => Options?.PageRoutes;

    public PreauthorizeNavigationService(PreauthorizeNavigationServiceOptions Options, ObjectAuthorization ObjectAuthorization)
    {
      this.ObjectAuthorization = ObjectAuthorization;
      this.Options = Options;
    }

    public bool? IsAuthorized(string path)
    {
      bool? result = null;
      path = path.Trim();
      var routeMatches = PageRoutes.Where(e => e.Value.Any(p => string.Equals(p.Trim(), path, StringComparison.InvariantCultureIgnoreCase)));
      if (routeMatches.Any()) //keep result null to indicate that no match was found
      {
        result = routeMatches.Any(e => IsAuthorized(e.Key) ?? false); //check each match (should ideally only be 1) for authorization
      }
      return result;
    }

    public bool? IsAuthorized<TComponent>() where TComponent : ComponentBase
    {
      return IsAuthorized(typeof(TComponent));
    }

    public bool? IsAuthorized(Type Type)
    {
      bool? result = null;
      if (PageRoutes.Keys.Contains(Type))
      {
        result = ObjectAuthorization.IsAuthorized(Type);
      }
      return result;
    }
  }

  public class PreauthorizeNavigationServiceOptions
  {
    private Dictionary<Type, List<string>> pageRoutes { get; set; }
    public IReadOnlyDictionary<Type, string[]> PageRoutes => pageRoutes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());

    public PreauthorizeNavigationServiceOptions(BlazorUIAssemblyService BlazorUIAssemblyService)
    {
      initializePageRoutes(BlazorUIAssemblyService);
    }

    private void initializePageRoutes(BlazorUIAssemblyService blazorUIAssemblyService)
    {
      var assemblies = new List<Assembly>() { Assembly.GetExecutingAssembly() }.Union(blazorUIAssemblyService?.Assemblies ?? new List<Assembly>());
      //RouteAttribute not inherited - See: https://github.com/dotnet/aspnetcore/issues/5529#issuecomment-492242663
      var pageComponents = assemblies.SelectMany(a => a.ExportedTypes)
                                 .Where(t => t.IsSubclassOf(typeof(ComponentBase)) && t.GetCustomAttributes(inherit: false).Any(a => typeof(Microsoft.AspNetCore.Components.RouteAttribute).IsAssignableFrom(a.GetType())));
      pageRoutes = pageComponents.ToDictionary(t => t, t => t.GetCustomAttributes(inherit: false).Where(a => typeof(RouteAttribute).IsAssignableFrom(a.GetType())).Select(a => ((RouteAttribute)a).Template).ToList()); //I'm not sure if this accounts for templates in the path, i.e. /path/to/page/{parameter1}/{parameter2} - I'm just going to assume that users arent going to try to
                                                                                                                                                                                                                        //Note: this would be a valid use case though when the route path contains custom templated values, like the culture code. but this implementation does not account for that
    }
  }

  public static class PreauthorizeNavigationServiceExtenstions
  {
    public static IServiceCollection AddPreauthorizeNavigationService(this IServiceCollection services)
    {
      services.TryAddSingleton<PreauthorizeNavigationServiceOptions>();
      services.TryAddScoped<PreauthorizeNavigationService>();
      return services;
    }
  }
}
