using System;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SarData.Auth.Saml;

namespace SarData.Auth
{
  public class SamlPluginLoader
  {
    public static Type GetSamlPluginType(IServiceCollection services, IConfiguration config, string contentRoot, ILogger logger)
    {
      PluginLoader loader = PluginLoader.CreateFromAssemblyFile(Path.Combine(contentRoot, "./plugins/SarData.Auth.Saml.Implementation.dll"),
                        sharedTypes: new[] { typeof(ILogger), typeof(IConfiguration), typeof(ClaimsPrincipal) });
      try
      {
        Assembly pluginDll = loader.LoadDefaultAssembly();

        Type configurator = pluginDll.GetType("SarData.Auth.Saml.SamlConfigurator");
        ((ISamlConfigurator)Activator.CreateInstance(configurator)).Configure(services, config, logger);

        Type implementationType = pluginDll.GetType("SarData.Auth.Saml.SamlImplementation");
        return implementationType;
      }
      catch (FileNotFoundException)
      {
        logger.LogWarning("SAML plugin not found. SAML SSO will not be available");
      }

      return typeof(SamlIdentityProvider);
    }
  }
}
