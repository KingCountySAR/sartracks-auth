using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SarData.Auth.Data;
using SarData.Auth.Models;
using SarData.Auth.Services;
using System.Reflection;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SarData.Auth.Identity;

namespace SarData.Auth
{
  public class Startup
  {
    public Startup(IConfiguration configuration, IHostingEnvironment env)
    {
      Configuration = configuration;
      this.env = env;
    }

    public IConfiguration Configuration { get; }

    private IHostingEnvironment env;
    private Uri siteRoot;
    private bool useMigrations = true;

    // TODO - Figure out how to get this into ApplicationDbContext
    public static string SqlDefaultSchema { get; private set; } = "auth";

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      string connectionString = Configuration.GetValue<string>("store:connectionString");
      string migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
      siteRoot = new Uri(Configuration.GetValue<string>("siteRoot"));

      Action<DbContextOptionsBuilder> configureDbAction = sqlBuilder => sqlBuilder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
      if (connectionString.ToLowerInvariant().StartsWith("filename="))
      {
        SqlDefaultSchema = null;
        useMigrations = false;
        configureDbAction = sqlBuilder => sqlBuilder.UseSqlite(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
      }

      if (env.IsDevelopment())
      {
        services.AddSingleton<IRemoteMembersService>(new LocalFileMembersService());
      }


      services.AddDbContext<ApplicationDbContext>(options => { configureDbAction(options); });
      services.AddTransient<IPasswordHasher<ApplicationUser>, LegacyPasswordHasher>();

      services.AddIdentity<ApplicationUser, IdentityRole>()
          .AddUserManager<LinkedMemberUserManager>()
          .AddSignInManager<LinkedMemberSigninManager>()
          .AddEntityFrameworkStores<ApplicationDbContext>()
          .AddDefaultTokenProviders();

      configueExternalLogins(services.AddAuthentication());

      // Add application services.
      if (env.IsDevelopment())
      {
        services.AddTransient<IMessagingService, TestMessagingService>();
      }
      else
      {
        services.AddTransient<IMessagingService, MessagingService>();
      }

      services.AddMvc();

      services.AddIdentityServer(options =>
        {
          options.PublicOrigin = siteRoot.GetLeftPart(UriPartial.Authority);
        })
        .AddDeveloperSigningCredential()
        .AddConfigurationStore(options =>
        {
          options.DefaultSchema = SqlDefaultSchema;
          options.ConfigureDbContext = configureDbAction;
        })
        .AddOperationalStore(options =>
        {
          // this adds the operational data from DB (codes, tokens, consents)
          options.DefaultSchema = SqlDefaultSchema;
          options.ConfigureDbContext = configureDbAction;

          // this enables automatic token cleanup. this is optional.
          options.EnableTokenCleanup = true;
        })
        .AddAspNetIdentity<ApplicationUser>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
      {
        bool needTables = false;

        foreach (var dbContext in new DbContext[]
        {
          serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>(),
          serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>(),
          serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
        })
        {
          var database = dbContext.Database;
          if (useMigrations)
          {
            database.Migrate();
          }
          else
          {
            var migrates = database.GetMigrations();
            var creator = database.GetService<IRelationalDatabaseCreator>();
            if (!creator.Exists())
            {
              creator.Create();
              needTables = true;
              database.ExecuteSqlCommand("CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL); ");
            }

            if (needTables)
            {
              //string productVersion = database.GetType().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
              //foreach (var migration in database.GetMigrations())
              //{
              //  database.ExecuteSqlCommand($"INSERT INTO __EFMigrationsHistory VALUES('{migration}', '{productVersion}')");
              //}
              creator.CreateTables();
            }
          }
        }
      }

      Action<IApplicationBuilder> configure = innerApp =>
      {

        if (env.IsDevelopment())
        {
          innerApp.UseBrowserLink();
          innerApp.UseDeveloperExceptionPage();
          innerApp.UseDatabaseErrorPage();
        }
        else
        {
          innerApp.UseExceptionHandler("/Home/Error");
        }

        innerApp.Use((context, next) =>
        {
          context.Request.Host = new Microsoft.AspNetCore.Http.HostString(siteRoot.Host, siteRoot.Port);
          return next();
        });

        innerApp.UseStaticFiles();

        innerApp.UseIdentityServer();

        innerApp.UseMvc(routes =>
        {
          routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
        });
      };

      string path = siteRoot.AbsolutePath.TrimEnd('/');
      if (string.IsNullOrWhiteSpace(path))
      {
        configure(app);
      }
      else
      {
        app.Map(path, configure);
      }
    }

    private void configueExternalLogins(AuthenticationBuilder authBuilder)
    {
      if (!string.IsNullOrWhiteSpace(Configuration["auth:facebook:appId"])
        && !string.IsNullOrWhiteSpace(Configuration["auth:facebook:appSecret"]))
      {
        authBuilder.AddFacebook(facebook =>
        {
          facebook.AppId = Configuration["auth:facebook:appId"];
          facebook.AppSecret = Configuration["auth:facebook:appSecret"];
        });
      }
      if (!string.IsNullOrWhiteSpace(Configuration["auth:google:clientId"])
        && !string.IsNullOrWhiteSpace(Configuration["auth:google:clientSecret"]))
      {
        authBuilder.AddGoogle(google =>
        {
          google.ClientId = Configuration["auth:google:clientId"];
          google.ClientSecret = Configuration["auth:google:clientSecret"];
        });
      }
      var oidcProviders = (Configuration["auth:oidc:providers"] ?? "").Split(",").Select(f => f.Trim());
      foreach (var provider in oidcProviders)
      {
        authBuilder.AddOpenIdConnect(provider, Configuration[$"auth:oidc:{provider}:caption"], oidc =>
        {
          oidc.Authority = Configuration[$"auth:oidc:{provider}:authority"];
          oidc.ClientId = Configuration[$"auth:oidc:{provider}:clientId"];
          oidc.ClientSecret = Configuration[$"auth:oidc:{provider}:clientSecret"];
        });
      }

    }
  }
}
