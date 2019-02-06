using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Data
{
  [Table("UnitMemberships")]
  public class UnitMembershipShimRow
  {
    public Guid Id { get; set; }
    public DateTime Activated { get; set; }
    public DateTime? EndTime { get; set; }

    [Column("Person_Id")]
    public Guid MemberId { get; set; }
    [ForeignKey("MemberId")]
    public MemberShimRow Member { get; set; }


    [Column("Status_Id")]
    public Guid StatusId { get; set; }
    [ForeignKey("StatusId")]
    public UnitStatusShimRow Status { get; set; }
  }
}
