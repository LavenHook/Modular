using Microsoft.AspNetCore.Builder;

namespace Modular.AspNetCore
{
  public abstract class EndpointModule : Module
  {
    public abstract string EndpointPrefix { get; }

    public abstract void ConfigureEndpoint(IApplicationBuilder app);
  }
}
