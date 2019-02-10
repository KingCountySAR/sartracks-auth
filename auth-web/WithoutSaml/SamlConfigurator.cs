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
      logger.LogWarning("Web site was compiled without support for SAML logins.");
      services.AddTransient<SamlImplementation>();
    }
  }
}