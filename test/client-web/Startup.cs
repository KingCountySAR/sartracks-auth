using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Internal.SarData.Client.Web
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddMvc();

      JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

      services.AddAuthentication(options =>
      {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
      })
      .AddCookie("Cookies")
      .AddOpenIdConnect("oidc", options =>
      {
        options.SignInScheme = "Cookies";
        options.RequireHttpsMetadata = false;
        options.Authority = "http://localhost:5100";
        options.ClientId = "sample-mvc";
        options.SaveTokens = true;
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseBrowserLink();
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
      }

      app.UseAuthentication();
      app.UseStaticFiles();

      app.UseMvc(routes =>
      {
        routes.MapRoute(
                  name: "default",
                  template: "{controller=Home}/{action=Index}/{id?}");
      });
    }
  }
}
