using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Data
{
  [Table("UnitStatus")]
  public class UnitStatusShimRow
  {
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
  }
}
