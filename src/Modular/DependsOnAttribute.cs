using System;
using System.Collections.Generic;
using System.Linq;

namespace Modular
{
  [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
  public class DependsOnAttribute : Attribute
  {
    private readonly Type[] dependsOn;

    //public Type[] DependsOn => dependsOn;
    public DependsOnAttribute(params Type[] DependsOn)
    {
      dependsOn = DependsOn
        .Where(d => typeof(Module).IsAssignableFrom(d) || d.GetCustomAttributes(typeof(ModuleAttribute), false).Any())
        .ToArray();
    }

    public Type[] DependsOn
    {
      get
      {
        return dependsOn.SelectMany(d => recursiveDependencies(d, new List<Type>())).Distinct().ToArray();//recursiveDependencies(GetType(), new List<Type>());
      }
    }

    private Type[] recursiveDependencies(Type currentType, List<Type> currentList) //The current list is essential to track that there are no circulardependencies
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
  }
}
