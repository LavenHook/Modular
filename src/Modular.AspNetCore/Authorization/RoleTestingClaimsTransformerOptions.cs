namespace Modular.AspNetCore.Authorization
{
  public class RoleTestingClaimsTransformerOptions
  {
    public class AdditionalUserRoles
    {
      public string UserName { get; set; }
      public string[] AdditionalRoles { get; set; }
    }

    public AdditionalUserRoles[] AdditionalRoles { get; set; }
  }
}
