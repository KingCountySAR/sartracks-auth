using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SarData.Accounts.Quartz;
using SarData.Accounts.Quartz.Jobs.GSuite;
using SarData.Common.Apis;
using SarData.Server;
using SarData.Server.Apis.Health;
using Serilog;

namespace SarData.Accounts
{
  public class Startup
  {
    private readonly IWebHostEnvironment env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
      Configuration = configuration;
      this.env = env;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddQuartzAndStart(Path.Combine(Configuration["local_files"] ?? env.ContentRootPath, "accounts-quartz.db"));

      services.AddSingleton<GSuiteApi>();

      services.AddControllersWithViews();

      // In production, the React files will be served from this directory
      services.AddSpaStaticFiles(configuration =>
      {
        configuration.RootPath = "ClientApp/build";
      });

      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
          string authority = Configuration["auth:authority"].TrimEnd('/');
          Log.Logger.Information("JWT Authority {0}", authority);
          options.Authority = authority;
          options.Audience = $"{authority}/resources";
          options.TokenValidationParameters.ValidIssuer = authority;
          options.RequireHttpsMetadata = env.EnvironmentName != Environments.Development;
        });

      var healthChecksBuilder = services.AddHealthChecks();

      services.AddSingleton<ITokenClient, DefaultTokenClient>();
      services.AddMessagingApi(Configuration, healthChecksBuilder);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseSarHealthChecks<Startup>();

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      app.UseQuartzDependencyInjection();

      app.UseHttpsRedirection()
          .UseStaticFiles()
          .UseSpaStaticFiles();

      app.UseRouting()
          .UseAuthentication()
          .UseAuthorization()
          .UseEndpoints(endpoints =>
          {
            endpoints.MapControllerRoute(
                      name: "default",
                      pattern: "{controller}/{action=Index}/{id?}");
          })
         .UseSpa(spa =>
          {
            spa.Options.SourcePath = "ClientApp";

            if (env.IsDevelopment())
            {
              spa.UseReactDevelopmentServer(npmScript: "start");
            }
          });
    }
  }
}
