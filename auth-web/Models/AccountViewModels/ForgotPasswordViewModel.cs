using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [EmailAddress]
        public string Email { get; set; }

        public string Username { get; set; }
    }
}
