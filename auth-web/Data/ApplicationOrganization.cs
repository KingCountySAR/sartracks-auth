using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Data
{
  public class ApplicationOrganization
  {
    [ForeignKey("ApplicationId")]
    public Application Application { get; set; }
    public Guid ApplicationId { get; set; }

    [Required]
    public string OrganizationId { get; set; }
  }
}
