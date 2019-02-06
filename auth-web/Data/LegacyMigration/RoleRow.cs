using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Data.LegacyMigration
{
  [Table("Roles")]
  public class RoleRow
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
  }
}
