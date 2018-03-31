using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SarData.Auth.Models.AccountViewModels
{
  public class ExternalLoginViewModel : IValidatableObject
  {
    [EmailAddress]
    public string Email { get; set; }

    [Phone]
    [Display(Name = "Cell Phone")]
    public string Phone { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
      if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(Phone))
      {
        yield return new ValidationResult($"Must specify either Email or Phone #.", new string[0]);
      }
    }
  }
}
