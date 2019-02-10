using System.Collections.Generic;
using ComponentSpace.Saml2.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SarData.Auth.Saml;

namespace SarData.Auth
{
  public static class SamlConfigurator
  {
    public static void AddSamlIfSupported(this IServiceCollection services, IConfiguration config, ILogger logger)
    {
      services.AddSaml(samlConfigurations =>
      {
        samlConfigurations.Configurations = new List<SamlConfiguration>()
          {
            new SamlConfiguration
            {
              LocalIdentityProviderConfiguration = new LocalIdentityProviderConfiguration
              {
                Name = config["siteRoot"] + "/SAML/SingleSignOnService",
                Description = "KCSARA Sign-In",
                SingleSignOnServiceUrl = config["siteRoot"] + "SAML/SingleSignOnService",
                LocalCertificates = new List<Certificate>()
                {
                  new Certificate()
                  {
                    String = config["auth:signingKey"],
                    Password = string.Empty
                  }
                }
              },
              PartnerServiceProviderConfigurations= new []
              {
                new PartnerServiceProviderConfiguration
                {
                  Name = config["auth:saml:facebook:name"],
                  Description = "Facebook @ Work",
                  WantAuthnRequestSigned = false,
                  SignAssertion = true,
                  SignSamlResponse = false,
                  NameIDFormat = "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress",
                  AssertionConsumerServiceUrl = config["auth:saml:facebook:acsUrl"],
                }
              }
            }
          };

      });
      services.AddTransient<SamlImplementation>();
    }
  }
}
