using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace Modular.AspNetCore.Authorization
{
  public class BlazorContextUser
  {
    private ILogger<BlazorContextUser> Logger { get; set; }
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    public BlazorContextUser(AuthenticationStateProvider AuthenticationStateProvider, ILogger<BlazorContextUser> Logger)
    {
      this.Logger = Logger;
      this.AuthenticationStateProvider = AuthenticationStateProvider;
    }

    public ClaimsPrincipal User
    {
      get
      {
        ClaimsPrincipal user = (Task.Factory.StartNew(async () => await AuthenticationStateProvider.GetAuthenticationStateAsync()).Unwrap().GetAwaiter().GetResult()?.User);
        if (user is null)
        {
          Logger.LogDebug($"{nameof(BlazorContextUser)} failed to authenticate a user");
        }
        else
        {
          Logger.LogDebug($"{nameof(BlazorContextUser)} successfully authenticated user [{user.Identity?.Name}]");
        }
        return user;
      }
    }
  }
}
