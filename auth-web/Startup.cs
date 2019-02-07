using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SarData.Auth.Data;
using SarData.Auth.Identity;
using SarData.Auth.Saml;
using SarData.Auth.Services;

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

    // TODO - Figure out how to get this into ApplicationDbContext
    public static string SqlDefaultSchema { get; private set; } = "auth";

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      siteRoot = new Uri(Configuration["siteRoot"]);
      Action<DbContextOptionsBuilder> configureDbAction = AddDatabases(services);

      //if (env.IsDevelopment() && (string.IsNullOrEmpty(Configuration["api:root"]) || string.IsNullOrEmpty(Configuration["api:key"])))
      //{
      //  servicesLogger.LogInformation("Will read members from local members.json file");
      //  services.AddSingleton<IRemoteMembersService>(new LocalFileMembersService());
      //}
      //else if (!(string.IsNullOrEmpty(Configuration["api:root"]) || string.IsNullOrEmpty(Configuration["api:key"])))
      //{
      //  servicesLogger.LogInformation("Will read members from API at " + Configuration["api:root"]);
      //  services.AddSingleton<IRemoteMembersService>(new LegacyApiMemberService(Configuration["api:root"], Configuration["api:key"]));
      //}

      services.AddSingleton<IRemoteMembersService>(new ShimMemberService(new MembershipShimDbContext(Configuration["store:connectionString"])));
      services.AddTransient(f => new Data.LegacyMigration.LegacyAuthDbContext(Configuration["store:connectionstring"]));

      services.AddTransient<IPasswordHasher<ApplicationUser>, LegacyPasswordHasher>();
      services.AddTransient<OidcSeeder>();

      services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
      {
        options.ClaimsIdentity.UserNameClaimType = JwtClaimTypes.Name;
      })
          .AddRoleManager<ApplicationRoleManager>()
          .AddUserManager<ApplicationUserManager>()
          .AddSignInManager<LinkedMemberSigninManager>()
          .AddEntityFrameworkStores<ApplicationDbContext>()
          .AddDefaultTokenProviders();

      services.ConfigureApplicationCookie(options =>
      {
        options.LoginPath = "/Login";
      });

      AddExternalLogins(services.AddAuthentication());

      // Add application services.
      //if (env.IsDevelopment())
      //{
      services.AddTransient<IMessagingService, TestMessagingService>();
      //}

      services.AddMvc();

      AddIdentityServer(services, configureDbAction);

      services.AddTransient(typeof(SamlIdentityProvider), SamlPluginLoader.GetSamlPluginType(services, Configuration, env.ContentRootPath, servicesLogger));
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
      services.AddDbContext<ApplicationDbContext>(options =>
      {
        options.EnableSensitiveDataLogging();
        configureDbAction(options);
      });
      return configureDbAction;
    }

    private void AddIdentityServer(IServiceCollection services, Action<DbContextOptionsBuilder> configureDbAction)
    {
      var identityServer = services.AddIdentityServer(options =>
      {
        options.PublicOrigin = siteRoot.GetLeftPart(UriPartial.Authority);
        options.UserInteraction.ErrorUrl = "/LoginError";
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
        .AddCustomAuthorizeRequestValidator<MultiOrganizationRequestValidator>()
        .AddAspNetIdentity<ApplicationUser>();

      if (string.IsNullOrEmpty(Configuration["auth:signingKey"]))
      {
        servicesLogger.LogWarning("Using development signing certificate");
        identityServer.AddDeveloperSigningCredential();
      }
      else
      {
        var cert = new X509Certificate2(Convert.FromBase64String(Configuration["auth:signingKey"]), string.Empty, X509KeyStorageFlags.MachineKeySet);
        byte[] encodedPublicKey = cert.PublicKey.EncodedKeyValue.RawData;
        File.WriteAllLines("signing-key-public.txt", new[] {
            "-----BEGIN PUBLIC KEY-----",
            Convert.ToBase64String(encodedPublicKey, Base64FormattingOptions.InsertLineBreaks),
            "-----END PUBLIC KEY-----",
        });
        servicesLogger.LogInformation($"Signing certificate {cert.FriendlyName} expiring {cert.GetExpirationDateString()}");
        identityServer.AddSigningCredential(cert);
      }
    }
  }
}
