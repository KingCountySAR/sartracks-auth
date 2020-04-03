using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SarData.Auth.Data;
using SarData.Auth.Identity;
using SarData.Auth.Models;
using SarData.Auth.Services;
using SarData.Common.Apis;
using SarData.Common.Apis.Messaging;
using SarData.Server;
using SarData.Server.Apis.Health;

namespace SarData.Auth
{
  public class Startup
  {
    public Startup(IConfiguration configuration, IWebHostEnvironment env, ILogger<Startup> logger)
    {
      Configuration = configuration;
      this.env = env;
      startupLogger = logger;
    }

    public IConfiguration Configuration { get; }

    private readonly ILogger startupLogger;
    private readonly IWebHostEnvironment env;
    private Uri siteRoot;
    private bool useMigrations = true;

    // TODO - Figure out how to get this into ApplicationDbContext
    public static string SqlDefaultSchema { get; private set; } = "auth";

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      string databaseString = Configuration["store:connectionString"];
      siteRoot = new Uri(Configuration["siteRoot"]);

      var healthChecksBuilder = services.AddHealthChecks()
        .AddSqlServer(
          connectionString: databaseString,
          healthQuery: "SELECT 1;",
          name: "sql",
          failureStatus: HealthStatus.Unhealthy,
          tags: new string[] { "db", "sql" }
        );

      Action<DbContextOptionsBuilder> configureDbAction = AddDatabases(services);

      services.AddSingleton(Configuration);
      services.AddApplicationInsightsTelemetry();

      services.AddSingleton<IRemoteMembersService>(new ShimMemberService(new MembershipShimDbContext(databaseString)));
      services.AddTransient(f => new Data.LegacyMigration.LegacyAuthDbContext(databaseString));

      services.AddTransient<IPasswordHasher<ApplicationUser>, LegacyPasswordHasher>();
      services.AddTransient<OidcSeeder>();

      services.Configure<CookiePolicyOptions>(options =>
      {
        options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
        options.OnAppendCookie = cookieContext => CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
        options.OnDeleteCookie = cookieContext => CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
      });

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
      services.AddMessagingApi(Configuration, healthChecksBuilder);

      services.AddCors(options =>
      {
        options.AddDefaultPolicy(builder =>
        {
          var list = Configuration.GetSection("corsOrigins")?.Get<List<string>>() ?? new List<string>();
          var siteRoot = Configuration["siteRoot"];
          if (!list.Contains(siteRoot)) list.Add(siteRoot);

          startupLogger.LogInformation($"Allowed CORS Origins: {string.Join(",", list)}");
          builder.AllowAnyHeader().AllowCredentials().WithOrigins(list.ToArray());
        });
      });

      services.AddMvc()
        .AddJsonOptions(options => options.JsonSerializerOptions.Setup());

      AddIdentityServer(services, configureDbAction);
      services.AddTransient<IProfileService, MemberProfileService>();

      services.AddSamlIfSupported(Configuration, startupLogger);

      // In production, the React files will be served from this directory
      services.AddSpaStaticFiles(configuration =>
      {
        configuration.RootPath = "frontend/build";
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseSarHealthChecks<Startup>();

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
          startupLogger.LogInformation($"Starting database {dbContext.GetType().Name} @ {database.GetDbConnection().DataSource}");
          //throw new InvalidOperationException("Trying to connect to " + dbContext.Database.GetDbConnection().DataSource);
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

        innerApp.UseCookiePolicy();
        innerApp.UseStaticFiles();
        innerApp.UseSpaStaticFiles();

        innerApp.UseRouting();
        innerApp.UseCors();
        innerApp.UseIdentityServer();
        innerApp.UseAuthorization();

        innerApp.UseEndpoints(endpoints =>
        {
          endpoints.MapControllers();
        });

        innerApp.UseSpa(spa =>
        {
          spa.Options.SourcePath = "frontend";

          if (env.IsDevelopment())
          {
            spa.UseReactDevelopmentServer(npmScript: "start");
          }
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

    private void CheckSameSite(HttpContext httpContext, CookieOptions options)
    {
      if (options.SameSite == SameSiteMode.None)
      {
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        if (DisallowsSameSiteNone(userAgent))
        {
          options.SameSite = SameSiteMode.Unspecified;
        }
      }
    }

    private bool DisallowsSameSiteNone(string userAgent)
    {
      // Cover all iOS based browsers here. This includes:
      // - Safari on iOS 12 for iPhone, iPod Touch, iPad
      // - WkWebview on iOS 12 for iPhone, iPod Touch, iPad
      // - Chrome on iOS 12 for iPhone, iPod Touch, iPad
      // All of which are broken by SameSite=None, because they use the iOS networking
      // stack.
      if (userAgent.Contains("CPU iPhone OS 12") ||
          userAgent.Contains("iPad; CPU OS 12"))
      {
        return true;
      }

      // Cover Mac OS X based browsers that use the Mac OS networking stack. 
      // This includes:
      // - Safari on Mac OS X.
      // This does not include:
      // - Chrome on Mac OS X
      // Because they do not use the Mac OS networking stack.
      if (userAgent.Contains("Macintosh; Intel Mac OS X 10_14") &&
          userAgent.Contains("Version/") && userAgent.Contains("Safari"))
      {
        return true;
      }

      // Cover Chrome 50-69, because some versions are broken by SameSite=None, 
      // and none in this range require it.
      // Note: this covers some pre-Chromium Edge versions, 
      // but pre-Chromium Edge does not require SameSite=None.
      if (userAgent.Contains("Chrome/5") || userAgent.Contains("Chrome/6"))
      {
        return true;
      }

      return false;
    }

    private void AddExternalLogins(AuthenticationBuilder authBuilder)
    {
      if (!string.IsNullOrWhiteSpace(Configuration["auth:external:facebook"]))
      {
        JsonElement settings = JsonSerializer.Deserialize<JsonElement>(Configuration["auth:external:facebook"]);
        authBuilder.AddFacebook(facebook =>
        {
          facebook.AppId = settings.GetString("appId");
          facebook.AppSecret = settings.GetString("appSecret");
        });
      }
      if (!string.IsNullOrWhiteSpace(Configuration["auth:external:google"]))
      {
        JsonElement settings = JsonSerializer.Deserialize<JsonElement>(Configuration["auth:external:google"]);
        authBuilder.AddGoogle(google =>
        {
          google.ClientId = settings.GetString("clientId");
          google.ClientSecret = settings.GetString("clientSecret");
        });
      }
      if (!string.IsNullOrWhiteSpace(Configuration["auth:external:oidc"]))
      {
        var oidcProviders = JsonSerializer.Deserialize<OidcConfig[]>(Configuration["auth:external:oidc"], new JsonSerializerOptions().Setup());
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
        options.EmitLegacyResourceAudienceClaim = true;
      })
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
        File.WriteAllLines(Path.Combine(Configuration["local_files"] ?? ".", "signing-key-public.txt"), new[] {
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
