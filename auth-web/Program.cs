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
      Console.WriteLine("Authentication site process " + System.Diagnostics.Process.GetCurrentProcess().Id);
      Console.Title = $"Auth {System.Diagnostics.Process.GetCurrentProcess().Id}";
      BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) =>    
      WebHost.CreateDefaultBuilder(args)
        .UseStartup<Startup>()
        .ConfigureAppConfiguration((context, config) =>
        {
          config.AddConfigFiles(context.HostingEnvironment.EnvironmentName);
        })
        .ConfigureLogging((context, logBuilder) =>
        {
          logBuilder.AddSarDataLogging(context.Configuration["local_files"] ?? context.HostingEnvironment.ContentRootPath, "auth");
        })
        .Build();
  }
}