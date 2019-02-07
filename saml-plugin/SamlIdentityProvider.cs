using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SarData.Auth.Saml
{
  public class SamlIdentityProvider
  {
    public virtual Task ReceiveSsoAsync()
    {
      throw new NotImplementedException("Please provide implementation library");
    }

    public virtual Task CompleteSsoAsync(ClaimsPrincipal principal)
    {
      throw new NotImplementedException("Please provide implementation library");
    }

    public virtual Task<string> GetPendingPartner()
    {
      throw new NotImplementedException("Please provide implementation library");
    }
  }
}
