using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Models
{
  public class RoleRoleMembership : IEquatable<RoleRoleMembership>
  {
    [ForeignKey("Parent")]
    [Required]
    public string ParentId { get; set; }
    public ApplicationRole Parent { get; set; }

    [ForeignKey("Child")]
    public string ChildId { get; set; }
    public ApplicationRole Child { get; set; }

    public bool IsDirect { get; set; }

    public bool Equals(RoleRoleMembership other)
    {
      return string.Equals(ParentId, other.ParentId) && string.Equals(ChildId, other.ChildId) && IsDirect == other.IsDirect;
    }

    public override int GetHashCode()
    {
      return $"{ParentId}\t{ChildId}\t{IsDirect}".GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (obj is RoleRoleMembership) return Equals((RoleRoleMembership)obj);
      return false;
    }


    public override string ToString()
    {
      string inherited = IsDirect ? " *" : "";
      return $"{ChildId} in {ParentId}{inherited}";
    }
  }
}
