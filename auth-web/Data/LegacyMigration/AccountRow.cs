using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SarData.Auth.Data.LegacyMigration
{
  [Table("Accounts")]
  public class AccountRow
  {
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public Guid? MemberId { get; set; }
    public string LockReason { get; set; }
    public DateTime? Locked { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime? PasswordDate { get; set; }

    public ICollection<LoginRow> Logins { get; set; }
  }
}
