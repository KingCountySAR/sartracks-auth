using System.Collections.Generic;
using System.Linq;
using ComponentSpace.Saml2.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SarData.Auth.Data;
using SarData.Auth.Models;
using SarData.Auth.Saml;

namespace SarData.Auth
{
  public static class SamlConfigurator
  {
    static Dictionary<SamlNameIdFormat, string> nameIdFormats = new Dictionary<SamlNameIdFormat, string>
    {
      { SamlNameIdFormat.Email, "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress" }
    };

    public static void AddSamlIfSupported(this IServiceCollection services, IConfiguration config, ILogger logger)
    {
      var partners = JsonConvert.DeserializeObject<SamlPartner[]>(string.IsNullOrWhiteSpace(config["auth:saml"]) ? "[]" : config["auth:saml"]);

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
              PartnerServiceProviderConfigurations= partners.Select(p => new PartnerServiceProviderConfiguration
              {
                Name = p.Name,
                Description = p.Description,
                WantAuthnRequestSigned = false,
                SignAssertion = true,
                SignSamlResponse = false,
                NameIDFormat = nameIdFormats[p.IdFormat],
                AssertionConsumerServiceUrl = p.ACS
              })
              .ToArray()
            }
          };
      });
      services.AddTransient<SamlImplementation>();
    }
  }
}
