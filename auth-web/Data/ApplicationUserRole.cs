using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Data
{
  public class ApplicationUserRole : IdentityUserRole<string>
  {
    public ApplicationUser User { get; set; }
    public ApplicationRole Role { get; set; }
  }
}
