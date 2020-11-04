using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modular.AspNetCore
{
  public abstract class EndpointModule : Module
  {
    public abstract string EndpointPrefix { get; }

    public abstract void ConfigureEndpoint(IApplicationBuilder app);
  }
}
