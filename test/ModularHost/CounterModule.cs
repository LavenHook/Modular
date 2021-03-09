using Microsoft.Extensions.DependencyInjection;
using ModularHost.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModularHost
{
  public class CounterModule : Modular.Module
  {
    public override string Name => nameof(CounterModule);

    public override string Description => "Configures dependencies for the Counter";

    public override void ConfigureServices(IServiceCollection services)
    {
      services.AddTransient<CounterDependency>();
    }
  }
}
