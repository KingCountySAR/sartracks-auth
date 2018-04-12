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
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace SarData.Auth
{
  public class Startup
  {

    public Startup(IConfiguration configuration, IHostingEnvironment env, ILoggerFactory logFactory)
    {
      Configuration = configuration;
      this.env = env;
      servicesLogger = logFactory.CreateLogger("Startup");
    }

    public IConfiguration Configuration { get; }

    private readonly ILogger servicesLogger;
    private readonly IHostingEnvironment env;
    private Uri siteRoot;
    private bool useMigrations = true;
    private bool useSaml = false;

    // TODO - Figure out how to get this into ApplicationDbContext
    public static string SqlDefaultSchema { get; private set; } = "auth";

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      siteRoot = new Uri(Configuration.GetValue<string>("siteRoot"));
      Action<DbContextOptionsBuilder> configureDbAction = AddDatabases(services);

      if (env.IsDevelopment() && (string.IsNullOrEmpty(Configuration["api:root"]) || string.IsNullOrEmpty(Configuration["api:key"])))
      {
        servicesLogger.LogInformation("Will read members from local members.json file");
        services.AddSingleton<IRemoteMembersService>(new LocalFileMembersService());
      }
      else
      {
        servicesLogger.LogInformation("Will read members from API at " + Configuration["api:root"]);
        services.AddSingleton<IRemoteMembersService>(new LegacyApiMemberService(Configuration["api:root"], Configuration["api:key"]));
      }


      services.AddTransient<IPasswordHasher<ApplicationUser>, LegacyPasswordHasher>();
      services.AddTransient<OidcSeeder>();

      services.AddIdentity<ApplicationUser, IdentityRole>()
          .AddUserManager<LinkedMemberUserManager>()
          .AddSignInManager<LinkedMemberSigninManager>()
          .AddEntityFrameworkStores<ApplicationDbContext>()
          .AddDefaultTokenProviders();

      AddExternalLogins(services.AddAuthentication());

      // Add application services.
      //if (env.IsDevelopment())
      //{
        services.AddTransient<IMessagingService, TestMessagingService>();
      //}

      services.AddMvc();

      AddIdentityServer(services, configureDbAction);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
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
            // Common case - SQL Server, etc
            database.Migrate();
          }
          else
          {
            // Dev / Sqlite database
            var migrates = database.GetMigrations();
            var creator = database.GetService<IRelationalDatabaseCreator>();
            if (!creator.Exists())
            {
              creator.Create();
              needTables = true;
              //database.ExecuteSqlCommand("CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL); ");
            }

            if (needTables)
            {
              creator.CreateTables();
            }
          }
        }

        var seeder = serviceScope.ServiceProvider.GetRequiredService<OidcSeeder>();
        seeder.Seed();
      }

      loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Information);

      Action<IApplicationBuilder> configure = innerApp =>
      {

        if (env.IsDevelopment())
        {
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
#if NET471
        if (useSaml) { innerApp.UseIdentityServerSamlPlugin(); }
#endif

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

    private void AddExternalLogins(AuthenticationBuilder authBuilder)
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
      var oidcProviders = (Configuration["auth:oidc:providers"] ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim());
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


    private Action<DbContextOptionsBuilder> AddDatabases(IServiceCollection services)
    {
      string connectionString = Configuration.GetValue<string>("store:connectionString");
      string migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

      Action<DbContextOptionsBuilder> configureDbAction = sqlBuilder => sqlBuilder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
      if (connectionString.ToLowerInvariant().StartsWith("filename="))
      {
        SqlDefaultSchema = null;
        useMigrations = false;
        configureDbAction = sqlBuilder => sqlBuilder.UseSqlite(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
      }
      services.AddDbContext<ApplicationDbContext>(options => { configureDbAction(options); });
      return configureDbAction;
    }

    private void AddIdentityServer(IServiceCollection services, Action<DbContextOptionsBuilder> configureDbAction)
    {
      var identityServer = services.AddIdentityServer(options =>
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

      bool useDevCert = false;
      if (string.IsNullOrEmpty(Configuration["auth:signingKey"]))
      {
        useDevCert = true;
        servicesLogger.LogWarning("Using development signing certificate");
        identityServer.AddDeveloperSigningCredential();
      }
      else
      {
        var cert = new X509Certificate2(Convert.FromBase64String(Configuration["auth:signingKey"]));
        servicesLogger.LogInformation($"Signing certificate {cert.FriendlyName} expiring {cert.GetExpirationDateString()}");
        identityServer.AddSigningCredential(cert);
      }

#if NET471
      string samlLicensee = Configuration["auth:saml:licensee"];
      string samlKey = Configuration["auth:saml:key"];
      if (!string.IsNullOrEmpty(samlLicensee) && !string.IsNullOrEmpty(samlKey))
      {
        if (!useDevCert)
        {
          servicesLogger.LogInformation("Setting up SAML for licensee " + samlLicensee);
          identityServer.AddSamlPlugin(options =>
          {
            options.Licensee = samlLicensee;
            options.LicenseKey = samlKey;
          })
          .AddInMemoryServiceProviders(new[] {
            new IdentityServer4.Saml.Models.ServiceProvider {
              EntityId = "https://www.facebook.com/company/726856840841575",
              AssertionConsumerServices =
              {
                  new IdentityServer4.Saml.Models.Service(IdentityServer4.Saml.SamlConstants.BindingTypes.HttpPost, "https://workplace.facebook.com/work/saml.php", 1)
              }
            }
          });
        }
        else
        {
          servicesLogger.LogError("Can't use SAML with the developer signing certificate");
        }
      }
#endif
    }

  }
}
