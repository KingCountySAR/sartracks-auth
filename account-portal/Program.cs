using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Quartz;
using SarData.Logging;
using Serilog;

namespace SarData.Accounts
{
  public class Program
  {
    public static void Main(string[] args)
    {
      IHost host = CreateHostBuilder(args).Build();
      host.Run();
      try
      {
        IScheduler scheduler = (IScheduler)host.Services.GetService(typeof(IScheduler));
        scheduler?.Shutdown();
      }
      catch(ObjectDisposedException)
      {
        // fine. we're shutting down anyway.
      }
      Log.CloseAndFlush();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
          .ConfigureLogging((context, logBuilder) =>
          {
            logBuilder.AddSarDataLogging(context.Configuration["local_files"] ?? context.HostingEnvironment.ContentRootPath, "accounts");
          })
          .ConfigureAppConfiguration((context, config) =>
          {
            config.AddConfigFiles(context.HostingEnvironment.EnvironmentName);
          })
          .ConfigureWebHostDefaults(webBuilder =>
          {
            webBuilder.UseStartup<Startup>();
          });
  }
}
