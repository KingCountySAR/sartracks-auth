﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace SarData.Auth.Models
{
  // Add profile data for application users by adding properties to the ApplicationUser class
  public class ApplicationUser : IdentityUser
  {
    [MaxLength(100)]
    public string MemberId { get; set; }

    [JsonIgnore]
    public bool IsMember {  get { return !string.IsNullOrEmpty(MemberId); } }
  }
}
