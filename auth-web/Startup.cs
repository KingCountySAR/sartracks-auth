using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.Services;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SarData.Auth.Data;
using SarData.Auth.Identity;
using SarData.Auth.Models;
using SarData.Auth.Services;
using SarData.Common.Apis;
using SarData.Common.Apis.Messaging;

namespace SarData.Auth
{
  public class Startup
  {

    public Startup(IConfiguration configuration, IHostingEnvironment env, ILogger<Startup> logger)
    {
      Configuration = configuration;
      this.env = env;
      startupLogger = logger;
    }

    public IConfiguration Configuration { get; }

    private readonly ILogger startupLogger;
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

      var authSetup = services.AddAuthentication()
      .AddJwtBearer(options =>
      {
        options.Authority = siteRoot.AbsoluteUri;
        options.Audience = "auth-api";
        options.SaveToken = true;
        options.RequireHttpsMetadata = !env.IsDevelopment();
      });

      AddExternalLogins(authSetup);

      services.AddSingleton<ITokenClient, LocalTokenClient>();

      string messagingUrl = Configuration["apis:messaging:url"];
      if (string.IsNullOrWhiteSpace(messagingUrl))
      {
        startupLogger.LogWarning("messaging API not configured. Using test implementation");
        services.AddTransient<IMessagingApi, TestMessagingService>();
      }
      else
      {
        services.ConfigureApi<IMessagingApi>("messaging", Configuration);
      }

      services.AddCors(options =>
      {
        options.AddDefaultPolicy(builder =>
        {
          builder.AllowAnyOrigin().AllowAnyHeader().AllowCredentials();
        });
      });
      services.AddMvc();

      AddIdentityServer(services, configureDbAction);
      services.AddTransient<IProfileService, MemberProfileService>();

      services.AddSamlIfSupported(Configuration, startupLogger);
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

        innerApp.UseCors();

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
      if (!string.IsNullOrWhiteSpace(Configuration["auth:external:facebook"]))
      {
        JObject settings = JsonConvert.DeserializeObject<JObject>(Configuration["auth:external:facebook"]);
        authBuilder.AddFacebook(facebook =>
        {
          facebook.AppId = settings["appId"].Value<string>();
          facebook.AppSecret = settings["appSecret"].Value<string>();
        });
      }
      if (!string.IsNullOrWhiteSpace(Configuration["auth:external:google"]))
      {
        JObject settings = JsonConvert.DeserializeObject<JObject>(Configuration["auth:external:google"]);
        authBuilder.AddGoogle(google =>
        {
          google.ClientId = settings["clientId"].Value<string>();
          google.ClientSecret = settings["clientSecret"].Value<string>();
        });
      }
      if (!string.IsNullOrWhiteSpace(Configuration["auth:external:oidc"]))
      {
        var oidcProviders = JsonConvert.DeserializeObject<OidcConfig[]>(Configuration["auth:external:oidc"]);
        foreach (var provider in oidcProviders)
        {
          authBuilder.AddOpenIdConnect(provider.Id, provider.Caption, oidc =>
          {
            oidc.Authority = provider.Authority;
            oidc.ClientId = provider.ClientId;
            oidc.ClientSecret = provider.ClientSecret;
          });
        }
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
        startupLogger.LogWarning("Using development signing certificate");
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
        startupLogger.LogInformation($"Signing certificate {cert.FriendlyName} expiring {cert.GetExpirationDateString()}");
        identityServer.AddSigningCredential(cert);
      }
    }
  }
}
