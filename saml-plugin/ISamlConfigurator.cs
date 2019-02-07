using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SarData.Auth.Saml
{
  public interface ISamlConfigurator
  {
    void Configure(IServiceCollection services, IConfiguration config, ILogger logger);
  }
}
