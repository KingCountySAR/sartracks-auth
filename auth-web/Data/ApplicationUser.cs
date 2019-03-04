﻿using System;
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

    [JsonIgnore]
    public bool IsMember { get { return !string.IsNullOrEmpty(MemberId); } }

    public DateTimeOffset Created { get; set; }

    public ICollection<UserOrganization> CustomOrganizations { get; set; }
  }
}