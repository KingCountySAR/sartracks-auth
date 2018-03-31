using System;
using System.ComponentModel.DataAnnotations;

namespace SarData.Auth.Data
{
  public class ExternalLoginCode
  {
    [Key]
    [MaxLength(256)]
    public string LoginSubject { get; set; }

    [Required]
    [MaxLength(16)]
    public string Code { get; set; }

    [MaxLength(100)]
    [Required]
    public string MemberId { get; set; }

    public DateTime ExpiresUtc { get; set; }
  }
}
