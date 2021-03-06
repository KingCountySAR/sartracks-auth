﻿using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ComponentSpace.Saml2;
using ComponentSpace.Saml2.Assertions;
using Microsoft.Extensions.Logging;

namespace SarData.Auth.Saml
{
  /// <summary>
  /// The SAML2 library is licensed and can't be checked into source control. This class provides an abstraction layer
  /// around that library. Using conditional logic in the project file, an alternate (null) implemenation can be compiled in if
  /// that library is not available.
  /// </summary>
  public class SamlImplementation
  {
    private readonly ISamlIdentityProvider samlIdentityProvider;
    private readonly ILogger<SamlImplementation> logger;

    public SamlImplementation(
      ISamlIdentityProvider samlIdentityProvider,
      ILogger<SamlImplementation> logger)
    {
      logger.LogInformation("Created SamlImplementation");
      this.samlIdentityProvider = samlIdentityProvider;
      this.logger = logger;
    }

    public async Task ReceiveSsoAsync()
    {
      logger.LogInformation("Receiving SSO");

      var something = await samlIdentityProvider.ReceiveSsoAsync();
    }

    public async Task<string> GetPendingPartner()
    {
      var status = await samlIdentityProvider.GetStatusAsync();
      return status.GetPartnerPendingResponse();
    }

    public async Task CompleteSsoAsync(ClaimsPrincipal principal)
    {
      var status = await samlIdentityProvider.GetStatusAsync();
      logger.LogInformation("Completing SAML SSO call");
      // Get the name of the logged in user.
      var userName = principal.Identity.Name;
      // Include claims as SAML attributes.
      var attributes = new List<SamlAttribute>();
      string email = userName;
      foreach (var claim in ((ClaimsIdentity)principal.Identity).Claims)
      {
        if (claim.Type == "email") userName = claim.Value;
      }
      // The user is logged in at the identity provider.
      // Respond to the authn request by sending a SAML response containing a SAML assertion to the SP.
      await samlIdentityProvider.SendSsoAsync(userName, attributes);
    }
  }
}