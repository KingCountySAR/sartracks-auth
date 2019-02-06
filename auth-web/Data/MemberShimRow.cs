using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Data
{
  [Table("Members")]
  public class MemberShimRow
  {
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public ICollection<MemberContactShimRow> Contacts { get; set; }
    public ICollection<UnitMembershipShimRow> Memberships { get; set; }
  }
}
