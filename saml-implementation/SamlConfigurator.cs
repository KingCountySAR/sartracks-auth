using System.Collections.Generic;
using ComponentSpace.Saml2.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SarData.Auth.Saml
{
  public class SamlConfigurator : ISamlConfigurator
  {
    public IConfiguration Configuration { get; private set; }

    public void Configure(IServiceCollection services, IConfiguration config, ILogger logger)
    {
      Configuration = config;
      services.AddSaml(ConfigureSaml);
    }

    private void ConfigureSaml(SamlConfigurations samlConfigurations)
    {
      samlConfigurations.Configurations = new List<SamlConfiguration>()
      {
        new SamlConfiguration
        {
          LocalIdentityProviderConfiguration = new LocalIdentityProviderConfiguration
          {
            Name = Configuration["siteRoot"] + "/SAML/SingleSignOnService",
            Description = "KCSARA Sign-In",
            SingleSignOnServiceUrl = Configuration["siteRoot"] + "SAML/SingleSignOnService",
            LocalCertificates = new List<Certificate>()
            {
              new Certificate()
              {
                String = Configuration["auth:signingKey"],
                Password = string.Empty
              }
            }
          },
          PartnerServiceProviderConfigurations= new []
          {
            new PartnerServiceProviderConfiguration
            {
              Name = Configuration["auth:saml:facebook:name"],
              Description = "Facebook @ Work",
              WantAuthnRequestSigned = false,
              SignAssertion = true,
              SignSamlResponse = false,
              NameIDFormat = "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress",
              AssertionConsumerServiceUrl = Configuration["auth:saml:facebook:acsUrl"],
            }
          }
        }
      };
    }
  }
}
