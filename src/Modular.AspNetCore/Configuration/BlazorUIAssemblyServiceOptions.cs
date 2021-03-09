using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Modular.AspNetCore.Configuration
{
  public class BlazorUIAssemblyServiceOptions
  {
    private List<Assembly> assemblies { get; set; }

    public Assembly[] Assemblies => assemblies.ToArray();

    public void Add(Assembly Assembly)
    {
      assemblies.Add(Assembly);
    }

    public void AddRange(IEnumerable<Assembly> Assemblies)
    {
      assemblies.AddRange(Assemblies);
    }

    public Assembly Remove(Assembly Assembly)
    {
      return assemblies.Remove(Assembly) ? Assembly : null;
    }

    public IEnumerable<Assembly> Remove(Func<Assembly, bool> removePredecate)
    {
      List<Assembly> removed = new List<Assembly>();
      Predicate<Assembly> OnePassMatch = (Assembly a) =>
      {
        bool matches = removePredecate(a);
        if (matches)
        {
          removed.Add(a);
        }
        return matches;
      };
      assemblies.RemoveAll(OnePassMatch);
      return removed.AsEnumerable();
    }

    public BlazorUIAssemblyServiceOptions()
    {
      assemblies = new List<Assembly>();
    }
  }
}
