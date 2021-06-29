using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace SarData.Auth.Data
{
  // Add profile data for application users by adding properties to the ApplicationUser class
  public class ApplicationUser : IdentityUser
  {
    [MaxLength(100)]
    public string MemberId { get; set; }

    [MaxLength(20)]
    public string D4HId { get; set; }

    [JsonIgnore]
    public bool IsMember { get { return !string.IsNullOrEmpty(MemberId); } }

    public DateTimeOffset? LastLogin { get; set; }

    public DateTimeOffset Created { get; set; }

    public ICollection<UserOrganization> CustomOrganizations { get; set; }

    [MaxLength(64)]
    public string FirstName { get; set; }

    [MaxLength(128)]
    public string LastName { get; set; }

    public string Name
    {
      get
      {
        string nameSpacer = (string.IsNullOrEmpty(LastName) || string.IsNullOrEmpty(FirstName)) ? string.Empty : " ";
        return $"{FirstName}{nameSpacer}{LastName}";
      }
    }
  }
}
