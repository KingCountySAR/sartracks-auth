using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SarData.Auth.Saml
{
  /// <summary>
  /// The SAML2 library is licensed and can't be checked into source control. This class provides an abstraction layer
  /// around that library. Using conditional logic in the project file, an alternate (null) implemenation can be compiled in if
  /// that library is not available.
  /// </summary>
  public class SamlImplementation
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
