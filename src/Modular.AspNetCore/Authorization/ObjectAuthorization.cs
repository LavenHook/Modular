using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Modular.AspNetCore.Authorization
{
  //TODO: what will be the best way to handle AllowAnonymousAttribute on an object? I don't like the way it's by standard authorization (allowAnonymous = everyone has access)
  public class ObjectAuthorization
  {
    public ClaimsPrincipal User { get; set; }
    public IAuthorizationService authorizationService { get; set; }

    public ObjectAuthorization(IIdentity User, IAuthorizationService authorizationService) : this(new ClaimsPrincipal(User), authorizationService)
    {
    }

    public ObjectAuthorization(ClaimsPrincipal User, IAuthorizationService authorizationService)
    {
      this.User = User;
      this.authorizationService = authorizationService;
    }

    public bool IsAuthorized(object obj)
    {
      return IsAuthorized(obj.GetType());
    }

    public bool IsAuthorized(Type Type)
    {
      bool isAuthorized = true;
      bool isAuthenticated = User?.Identities?.Any(i => i.IsAuthenticated) ?? false;
      if (isAuthenticated)
      {
        foreach (var authAttribute in Type.GetCustomAttributes(inherit: true).Where(a => typeof(AuthorizeAttribute).IsAssignableFrom(a.GetType())).Cast<AuthorizeAttribute>())
        {
          var roles = authAttribute.Roles?.Split(",").Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

          //policies/roles within a single AuthorizeAttribute are ORed, seperate authorizedAttributes are ANDed
          bool rolesWereProvided = roles is IEnumerable<string> && roles.Any();
          bool policyWasProvided = !string.IsNullOrWhiteSpace(authAttribute.Policy);
          bool passes = !rolesWereProvided && !policyWasProvided;

          //Check Role authorizaiton, if necessary
          //bool isAuthorizedForRoles = roles is null || !roles.Any() || roles.Any(r => User.IsInRole(r));
          if (!passes && rolesWereProvided) //short circuit this test, if already passing
          {
            var schemes = authAttribute.AuthenticationSchemes?.Split(",").Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)) ?? new List<string>();

            var roleRequirement = new RolesAuthorizationRequirement(roles);
            AuthorizationPolicy rolePolicy = new AuthorizationPolicy(new List<IAuthorizationRequirement>() { roleRequirement }, schemes);
            passes = (Task.Factory.StartNew(async () => await authorizationService.AuthorizeAsync(User, rolePolicy)).Unwrap().GetAwaiter().GetResult()?.Succeeded ?? false);
          }

          //check Policy authorization, if necessary
          if (!passes && policyWasProvided) //short circuit this test, if already passing
          {
            passes = (Task.Factory.StartNew(async () => await authorizationService.AuthorizeAsync(User, authAttribute.Policy)).Unwrap().GetAwaiter().GetResult()?.Succeeded ?? false);
          }

          //if no roles, and no policy, then pass
          //if roles or policy, then at least one must succeed
          isAuthorized = isAuthorized && ((!rolesWereProvided && !policyWasProvided) || passes); //what a PITA!
          if (!isAuthorized)
          {
            break;
          }
        }
      }
      return isAuthorized;
    }

    public IEnumerable<object> AuthorizationFilter(ClaimsPrincipal User, IAuthorizationService authorizationService, IEnumerable<object> objects)
    {
      return objects.Where(o => IsAuthorized(o)).ToList();
    }
  }
}
