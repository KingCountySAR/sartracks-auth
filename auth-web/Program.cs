using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SarData.Auth
{
  public class Program
  {
    public static void Main(string[] args)
    {
      BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) =>
      WebHost.CreateDefaultBuilder(args)
        .UseStartup<Startup>()
        .ConfigureAppConfiguration(config =>
        {
          config.AddJsonFile("appsettings.json", true, false)
                .AddJsonFile("appsettings.local.json", true, false)
                .AddEnvironmentVariables();
        })
        .Build();
  }
}
