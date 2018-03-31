using SarData.Auth.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SarData.Auth.Services
{
  public interface IRemoteMembersService
  {
    Task<RemoteMember> GetMember(string id);
    Task<List<RemoteMember>> FindByEmail(string email);
    Task<List<RemoteMember>> FindByPhone(string phone);
  }
}
