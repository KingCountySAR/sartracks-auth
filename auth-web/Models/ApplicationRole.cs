using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace SarData.Auth.Models
{
  public class ApplicationRole : IdentityRole
  {
    public string Description { get; set; }
    public ICollection<RoleRoleMembership> Ancestors { get; set; } = new List<RoleRoleMembership>();
    public ICollection<ApplicationUserRole> UserMembers { get; set; } = new List<ApplicationUserRole>();
  }
}
