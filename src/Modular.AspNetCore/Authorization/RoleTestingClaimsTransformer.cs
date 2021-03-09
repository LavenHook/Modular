using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Modular.AspNetCore.Authorization
{
  /// <summary>
  ///
  /// </summary>
  /// <remarks>
  /// Use this class to transform claims programmatically, especially for the ability to test claim/policy requirements in different environments
  /// </remarks>
  // @SEE: https://benfoster.io/blog/customising-claims-transformation-in-aspnet-core-identity
  // @SEE: https://stackoverflow.com/questions/45709296/claims-transformation-support-missing-in-asp-net-core-2-0
  // @SEE: https://philipm.at/2018/aspnetcore_claims_with_windowsauthentication.html
  public class RoleTestingClaimsTransformer : IClaimsTransformation
  {
    private RoleTestingClaimsTransformerOptions Options { get; set; }

    public RoleTestingClaimsTransformer(IOptionsMonitor<RoleTestingClaimsTransformerOptions> options)
    {
      this.Options = options?.CurrentValue;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
      #region CAUTION

      //@CAUTION: following the note listed at https://philipm.at/2018/aspnetcore_claims_with_windowsauthentication.html
      // and creating a new principal will cause problems (possibly async race problems)
      // Instead, just use the existing principal, but add the roles that the user is not already in

      #endregion CAUTION

      var identity = (ClaimsIdentity)principal.Identity;

      //// create a new ClaimsIdentity copying the existing one
      //var claimsIdentity = new ClaimsIdentity(
      //    identity.Claims,
      //    identity.AuthenticationType,
      //    identity.NameClaimType,
      //    identity.RoleClaimType);

      foreach (string role in Options.AdditionalRoles?.FirstOrDefault(a => string.Equals(a.UserName, principal.Identity.Name, System.StringComparison.InvariantCultureIgnoreCase))?.AdditionalRoles?.Where(r => !principal.IsInRole(r))?.ToArray() ?? new string[] { })
      {
        identity.AddClaim(new Claim(identity.RoleClaimType, role));
      }

      // create a new ClaimsPrincipal in observation
      // of the documentation note
      //return Task.FromResult(new ClaimsPrincipal(claimsIdentity));
      return Task.FromResult(principal);
    }
  }
}
