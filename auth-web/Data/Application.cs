using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace SarData.Auth.Data
{
  public class Application
  {
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    public string Url { get; set; }

    public byte[] Logo { get; set; }

    public ICollection<ApplicationOrganization> Organizations { get; set; }
  }
}
