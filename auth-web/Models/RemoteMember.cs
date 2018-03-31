using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Models
{
  public class RemoteMember
  {
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PrimaryEmail { get; set; }
    public string PrimaryPhone { get; set; }

    public bool IsActive { get; set; }
  }
}
