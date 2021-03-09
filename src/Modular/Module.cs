using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Modular
{
  public abstract class Module
  {
    public abstract string Name { get; }
    public abstract string Description { get; }

    private int? priority { get; set; }

    public int Priority
    {
      get
      {
        if (priority is null)
        {
          priority = this.DependencyPriority();
        }
        return priority.Value;
      }
    }

    protected IConfiguration Configuration { get; private set; }
    protected IHostEnvironment Environment { get; private set; }

    private Module()
    {
    }

    public Module(IHostEnvironment Environment = null, IConfiguration Configuration = null)
    {
      this.Environment = Environment;
      this.Configuration = Configuration;
    }

    public abstract void ConfigureServices(IServiceCollection services);
  }
}
