using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;

namespace SarData.Accounts.Quartz.Jobs.GSuite
{
  public class GSuiteApi
  {
    private readonly string delegateAdmin;
    private readonly string secretsJson;

    public GSuiteApi(IConfiguration config)
    {
      delegateAdmin = config["gsuite:adminDelegate"];
      secretsJson = File.ReadAllText(Path.Combine(config["local_files"] ?? ".", "gsuite_client_secrets.json"));
    }

    public GmailService GetGmailService(string userEmail)
    {

      GoogleCredential credential = GoogleCredential.FromJson(secretsJson)
        .CreateScoped(new[] {
          GmailService.Scope.GmailSettingsBasic,
          GmailService.Scope.GmailSettingsSharing
        })
        .CreateWithUser(userEmail);

      JsonElement secrets = JsonSerializer.Deserialize<JsonElement>(secretsJson);

      var service = new GmailService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = credential,
        ApplicationName = secrets.GetProperty("project_id").GetString()
      });

      return service;
    }

    public DirectoryService GetDirectoryService()
    {
      GoogleCredential credential = GoogleCredential.FromJson(secretsJson)
        .CreateScoped(new[] {
          DirectoryService.Scope.AdminDirectoryUser
        })
        .CreateWithUser(delegateAdmin);
      

      JsonElement secrets = JsonSerializer.Deserialize<JsonElement>(secretsJson);

      var service = new DirectoryService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = credential,
        ApplicationName = secrets.GetProperty("project_id").GetString()
      });

      return service;
    }
  }
}
