using System;
using System.Collections.Generic;
using System.Linq;

namespace Modular
{
  public static class ModuleExtensions
  {
    public static Type[] DependsOn(this object @this)
    {
      return @this.DependsOn(true);
    }

    public static Type[] DependsOn(this object @this, bool ExclueNonModules)
    {
      return DependsOn(@this?.GetType() ?? typeof(object), ExclueNonModules);
    }

    public static Type[] DependsOn<TModuleType>()
    {
      return DependsOn<TModuleType>(true);
    }

    public static Type[] DependsOn<TModuleType>(bool ExclueNonModules)
    {
      return DependsOn(typeof(TModuleType), ExclueNonModules);
    }

    public static Type[] DependsOn(this Type @this)
    {
      return DependsOn(@this, true);
    }

    public static Type[] DependsOn(this Type @this, bool ExclueNonModules)
    {
      var allDependencies = recursiveDependencies(@this ?? typeof(object), new List<Type>());
      if (ExclueNonModules)
      {
        allDependencies = allDependencies.Where(t => t.GetCustomAttributes(typeof(ModuleAttribute), true).Any() || typeof(Module).IsAssignableFrom(t)).ToArray();
      }
      return allDependencies;
    }

    private static Type[] recursiveDependencies(Type currentType, List<Type> currentList) //The current list is essential to track that there are no circulardependencies
    {
      List<Type> results = new List<Type>();
      var currentDependsOnAttributes = currentType.GetCustomAttributes(typeof(DependsOnAttribute), true).Select(o => o as DependsOnAttribute);
      results.AddRange(currentDependsOnAttributes?.SelectMany(d => d.DependsOn) ?? new List<Type>());

      results = results.Distinct().Except(results.Where(t => currentList.Contains(t))).ToList();

      IEnumerable<Type> newTypes = results.SelectMany(t => recursiveDependencies(t, results)).ToList();

      results.AddRange(newTypes);
      results.Add(currentType);

      return results.Distinct().ToArray();
    }

    internal static int DependencyPriority(this object @this)
    {
      return DependencyPriority(@this?.GetType() ?? typeof(object));
    }

    //TODO: This /*is*/ may be horribly inefficient - probably big-O (n^2) - needs to be made more efficient
    //There *may* also be a circular dependency problem
    internal static int DependencyPriority(Type currentType)
    {
      int result = 0;
      var currentDependsOnAttributes = currentType.GetCustomAttributes(typeof(DependsOnAttribute), true).Select(o => o as DependsOnAttribute);
      if (currentDependsOnAttributes.Any())
      {
        result = currentDependsOnAttributes.SelectMany(d => d.DependsOn).Max(t => DependencyPriority(t)) + 1;
      }

      return result;
    }
  }
}
