using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SarData.Auth.Data.LegacyMigration
{
  [Table("AccountRoles")]
  public class AccountRoleRow
  {
    public Guid AccountRow_Id { get; set; }
    public string RoleRow_Id { get; set; }

    [ForeignKey("RoleRow_Id")]
    public RoleRow Role { get; set; }
  }
}
