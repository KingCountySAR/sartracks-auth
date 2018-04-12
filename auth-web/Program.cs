using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace SarData.Auth
{
  public class Program
  {
    public static void Main(string[] args)
    {
      BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) {
      var builder = WebHost.CreateDefaultBuilder(args);
      var insightsKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
      if (!string.IsNullOrWhiteSpace(insightsKey))
      {
        builder = builder.UseApplicationInsights(insightsKey);
      }

      return builder
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
}
