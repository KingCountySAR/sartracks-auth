using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SarData.Auth.Data;
using SarData.Auth.Models;
using SarData.Common.Apis.Messaging;

namespace SarData.Auth.Controllers
{
  public class HomeController : Controller
  {
    private readonly ApplicationDbContext db;

    public HomeController(ApplicationDbContext db)
    {
      this.db = db;
    }

    //[HttpGet("/")]
    //public async Task<IActionResult> Index()
    //{
    //  if (User.Identity.IsAuthenticated)
    //  {
    //    string userId = User.FindFirst("sub").Value;
    //    var userOrgs = await db.Users.Where(f => f.Id == userId).SelectMany(f => f.CustomOrganizations).Select(f => f.OrganizationId).ToListAsync();
    //    var apps = await db.Applications.Where(f => f.Organizations.Count == 0 || f.Organizations.Any(g => userOrgs.Contains(g.OrganizationId))).ToListAsync();
    //    ViewData["apps"] = apps;
    //    return View();
    //  }
    //  return View();
    //}

    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet("/react-config")]
    public async Task<IActionResult> ReactConfig([FromServices] IConfiguration config)
    {
      object me = null;
      object oidc = null;
      if (User.Identity.IsAuthenticated)
      {
        string userId = User.FindFirstValue("sub");
        me = new
        {
          UserId = userId,
          Apps = await AccountsApiController.GetUserApplications(db, userId, new Dictionary<string, int>(), new Dictionary<string, string>(), null, null)
        };
        oidc = new
        {
          User = new
          {
            Profile = new
            {
              Name = User.FindFirstValue("name"),
              Sub = userId
            }
          }
        };
      }

      return Content("window.reactConfig = " + JsonConvert.SerializeObject(new
      {
        auth = new
        {
          authority = "/",
          client_id = config["apis:frontend:client_id"]
        },
        apis = new
        {
          data = new
          {
            url = (config["apis:database"] ?? "").TrimEnd('/')
          }
        },
        me,
        oidc
      }, new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() }));
    }

    [Authorize]
    [HttpGet("/test/email/{to}")]
    public async Task<IActionResult> TestEmail(string to, [FromServices] IMessagingApi messaging)
    {
      await messaging.SendEmail(to, "Test email from auth service", "This is a test mail. I hope it made it through");
      return Content("OK");
    }
    [HttpGet("/generatesecret")]
    public object GenerateSecret([FromQuery] string key)
    {
      char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
      StringBuilder sb = new StringBuilder();
      var randomBytes = new byte[64];
      new RNGCryptoServiceProvider().GetBytes(randomBytes);
      for (int i=0; i<64;i++)
      {
        sb.Append(chars[randomBytes[i] % chars.Length]);
      }

      var secret = key ?? sb.ToString();
      return new
      {
        Secret = secret,
        Hash = secret.Sha256()
      };
    }
  }
}
