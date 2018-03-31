using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SarData.Auth.Models.AccountViewModels
{
  public class ExternalVerifyViewModel
  {
    [Required]
    public string Code { get; set; }
  }
}
