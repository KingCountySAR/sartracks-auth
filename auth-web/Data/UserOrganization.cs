using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Data
{
  public class UserOrganization
  {
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; }
    [Required]
    public string UserId { get; set; }

    [Required]
    public string OrganizationId { get; set; }
  }
}
