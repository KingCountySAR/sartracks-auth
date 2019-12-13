using System;
using Microsoft.AspNetCore.Identity;

namespace SarData.Auth.Data
{
  public class ApplicationUserRole : IdentityUserRole<string>
  {
    public ApplicationUser User { get; set; }
    public ApplicationRole Role { get; set; }
    public bool Assigned { get; set; }
  }
}
