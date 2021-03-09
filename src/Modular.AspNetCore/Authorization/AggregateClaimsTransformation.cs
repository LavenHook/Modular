using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace Modular.AspNetCore.Authorization
{
  public class AggregateClaimsTransformation : IClaimsTransformation
  {
    private Func<IEnumerable<IClaimsTransformation>> GetTransformations { get; set; }

    public AggregateClaimsTransformation(Func<IEnumerable<IClaimsTransformation>> Transformations)
    {
      GetTransformations = Transformations;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
      var tasks = GetTransformations().Select(t => t.TransformAsync(principal));
      await Task.WhenAll(tasks);
      return principal;
    }
  }
}
