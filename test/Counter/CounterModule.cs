using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CounterRCL
{
  public class CounterModule : Modular.Core.Module
  {
    public override string Name => nameof(CounterModule);

    public override string Description => "Configures dependencies for the Counter";

    public override void ConfigureServices(IServiceCollection services)
    {
      services.AddTransient<CounterDependency>();
    }
  }
}
