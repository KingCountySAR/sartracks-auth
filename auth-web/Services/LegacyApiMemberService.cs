using Newtonsoft.Json;
using SarData.Auth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SarData.Auth.Services
{
  public class LegacyApiMemberService : IRemoteMembersService
  {
    readonly string authKey;
    readonly string apiRoot;

    public LegacyApiMemberService(string apiRoot, string apiKey)
    {
      authKey = apiKey;
      this.apiRoot = apiRoot.TrimEnd('/') + "/auth-support/";
    }

    private async Task<string> Get(string url)
    {
      using (HttpClient client = new HttpClient())
      {
        client.DefaultRequestHeaders.Add("X-Auth-Service-Key", authKey);
        return await client.GetStringAsync(apiRoot + url);
      }
    }

    public async Task<List<RemoteMember>> FindByEmail(string email)
    {
      return JsonConvert.DeserializeObject<List<ApiRemoteMember>>(await Get("byemail/" + WebUtility.UrlEncode(email))).Cast<RemoteMember>().ToList();
    }

    public async Task<List<RemoteMember>> FindByPhone(string phone)
    {
      return JsonConvert.DeserializeObject<List<ApiRemoteMember>>(await Get("byphone/" + WebUtility.UrlEncode(phone))).Cast<RemoteMember>().ToList();
    }

    public async Task<RemoteMember> GetMember(string id)
    {
      return JsonConvert.DeserializeObject<ApiRemoteMember>(await Get(WebUtility.UrlEncode(id.ToString())));
    }

    public class ApiRemoteMember : RemoteMember
    {
      private NameIdPair[] units;

      public NameIdPair[] Units
      {
        get
        {
          return units;
        }
        set
        {
          units = value;
          IsActive = value.Length > 0;
        }
      }

      public class NameIdPair
      {
        public Guid Id { get; set; }
        public string Name { get; set; }
      }
    }

  }
}
