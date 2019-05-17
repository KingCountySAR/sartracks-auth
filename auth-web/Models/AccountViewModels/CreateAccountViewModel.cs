using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Models.AccountViewModels
{
    public class CreateAccountViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [RegularExpression("^[a-zA-Z][a-zA-Z\\.\\-]+$")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Display Name")]
        public string Name { get; set; }

        [Display(Name = "Member ID")]
        [MinLength(5)]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.")]
        public string MemberId { get; set;}
    }
}
