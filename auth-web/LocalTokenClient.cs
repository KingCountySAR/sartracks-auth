using System.Threading.Tasks;
using IdentityServer4;
using SarData.Common.Apis;

namespace SarData.Auth
{
  public class LocalTokenClient : ITokenClient
  {
    private readonly IdentityServerTools tools;

    public LocalTokenClient(IdentityServerTools tools)
    {
      this.tools = tools;
    }

    public Task<string> GetToken(string scope)
    {
      return tools.IssueClientJwtAsync("auth-server", 180, new[] { scope }, new string[0]);
    }
  }
}
