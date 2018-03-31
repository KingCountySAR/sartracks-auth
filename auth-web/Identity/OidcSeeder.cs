using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SarData.Auth.Identity
{
  public class OidcSeeder
  {
    private readonly ConfigurationDbContext db;
    private readonly IFileInfo file;
    private readonly ILogger<OidcSeeder> log;

    public OidcSeeder(ConfigurationDbContext db, IHostingEnvironment env, ILogger<OidcSeeder> log)
    {
      this.db = db;
      file = env.ContentRootFileProvider.GetFileInfo("seed-oidc.json");
      this.log = log;
    }

    public void Seed()
    {
      if (file.Exists && !db.Clients.Any())
      {
        log.LogInformation("Beginning seed of OIDC config database ...");
        OidcSeedInfo info;
        using (var read = file.CreateReadStream())
        {
          using (var text = new StreamReader(read))
          {
            using (var json = new JsonTextReader(text))
            {
              info = new JsonSerializer().Deserialize<OidcSeedInfo>(json);
            }
          }
        }

        foreach (var client in info.Clients)
        {
          client.ClientSecrets = client.ClientSecrets.Select(s =>
          new Secret(s.Value.Sha256())
          ).ToList();
          db.Clients.Add(client.ToEntity());
        }
        foreach (var api in info.ApiResources)
        {
          db.ApiResources.Add(api.ToEntity());
        }

        db.IdentityResources.Add(new IdentityResources.OpenId().ToEntity());
        db.IdentityResources.Add(new IdentityResources.Profile().ToEntity());
        db.SaveChanges();
        int c = 0;
        foreach (var identity in info.IdentityResources)
        {
          identity.Name = (c++).ToString();
          db.IdentityResources.Add(identity.ToEntity());
        }

        db.SaveChanges();
      }
      else
      {
        log.LogInformation("No seed-oidc.json file or found existing OIDC clients. Will not seed.");
      }
    }

    public class OidcSeedInfo
    {
      public List<Client> Clients { get; set; }
      public List<IdentityResource> IdentityResources { get; set; }
      public List<ApiResource> ApiResources { get; set; }
    }
  }
}
