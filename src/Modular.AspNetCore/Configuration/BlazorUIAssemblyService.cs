using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace Modular.AspNetCore.Configuration
{
  public class BlazorUIAssemblyService
  {
    private BlazorUIAssemblyServiceOptions Options { get; set; }

    public IEnumerable<Assembly> Assemblies => Options.Assemblies.AsEnumerable();

    public BlazorUIAssemblyService(IOptions<BlazorUIAssemblyServiceOptions> options)
    {
      this.Options = options?.Value;
    }
  }
}
