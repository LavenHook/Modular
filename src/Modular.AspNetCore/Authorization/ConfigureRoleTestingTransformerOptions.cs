using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Modular.AspNetCore.Authorization
{
  public class ConfigureRoleTestingTransformerOptions : IConfigureOptions<RoleTestingClaimsTransformerOptions>
  {
    private IConfiguration Configuration { get; set; }

    public ConfigureRoleTestingTransformerOptions(IConfiguration configuration)
    {
      this.Configuration = configuration;
    }

    public void Configure(RoleTestingClaimsTransformerOptions options)
    {
      options.AdditionalRoles = Configuration.GetSection($"{nameof(RoleTestingClaimsTransformerOptions.AdditionalUserRoles)}").GetChildren()
        .Select(c => new RoleTestingClaimsTransformerOptions.AdditionalUserRoles()
        {
          UserName = c.Key,
          AdditionalRoles = c.GetChildren().Select(v => v.Value).ToArray()
        }).ToArray();
    }
  }
}
