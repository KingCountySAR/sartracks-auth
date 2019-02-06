using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Data
{
  [Table("PersonContacts")]
  public class MemberContactShimRow
  {
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }

    [ForeignKey("MemberId")]
    public MemberShimRow Member { get; set; }

    [Column("Person_Id")]
    public Guid MemberId { get; set; }

    public int Priority { get; set; }
  }
}
