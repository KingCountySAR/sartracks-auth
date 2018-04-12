using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SarData.Auth.Models;

namespace SarData.Auth.Services
{
  public class LocalFileMembersService : IRemoteMembersService
  {
    public async Task<RemoteMember> GetMember(string id)
    {
      return (await ReadMembers()).Where(f => f.Id == id).SingleOrDefault();
    }

    public async Task<List<RemoteMember>> FindByEmail(string email)
    {
      email = email.ToLowerInvariant();
      return (await ReadMembers()).Where(f => f.PrimaryEmail.ToLowerInvariant() == email).OrderBy(f => f.LastName).ThenBy(f => f.FirstName).ToList();
    }

    public async Task<List<RemoteMember>> FindByPhone(string phone)
    {
      Regex matcher = new Regex("[^\\d]", RegexOptions.Compiled);
      phone = matcher.Replace(phone, string.Empty);
      return (await ReadMembers()).Where(f => matcher.Replace(f.PrimaryPhone, string.Empty) == phone).OrderBy(f => f.LastName).ThenBy(f => f.FirstName).ToList();
    }

    private Task<List<RemoteMember>> ReadMembers()
    {
      return Task.FromResult(JsonConvert.DeserializeObject<List<RemoteMember>>(File.ReadAllText("members.json")));
    }
  }
}
