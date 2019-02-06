using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SarData.Auth.Data.LegacyMigration
{
  [Table("ExternalLogins")]
  public class LoginRow
  {    
    public string Provider { get; set; }
    public string ProviderId { get; set; }
   
    public Guid AccountId { get; set; }
    [ForeignKey("AccountId")]
    public AccountRow Account { get; set; }

    public DateTime? Created { get; set; }
  }
}
