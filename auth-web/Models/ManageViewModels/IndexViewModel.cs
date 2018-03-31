using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Models.ManageViewModels
{
  public class IndexViewModel
  {
    [RegularExpression("[A-Za-z]+[A-Za-z0-9_\\.-]+", ErrorMessage = "Must be 3-26 characters. Allowed characters: A-Z, 0-9, and _, ., -")]
    [StringLength(26, MinimumLength = 3)]
    [Display(Description = "Usernames that are based on your name (first or last) are preferred.")]
    public string Username { get; set; }

    public bool IsEmailConfirmed { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Phone]
    [Display(Name = "Phone number")]
    public string PhoneNumber { get; set; }

    public string StatusMessage { get; set; }

    public bool IsMember { get; internal set; }
  }
}
