using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SarData.Logging;

namespace SarData.Auth
{
  public class Program
  {
    public static void Main(string[] args)
    {
      BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args)
    {
      var builder = WebHost.CreateDefaultBuilder(args);
      var insightsKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
      if (!string.IsNullOrWhiteSpace(insightsKey))
      {
        builder = builder.UseApplicationInsights(insightsKey);
      }

      return builder
        .UseStartup<Startup>()
        .ConfigureAppConfiguration((context, config) =>
        {
          config.AddConfigFiles(context.HostingEnvironment.EnvironmentName);
        })
        .ConfigureLogging(logBuilder =>
        {
          logBuilder.AddSarDataLogging();
        })
        .Build();
    }
  }
}
