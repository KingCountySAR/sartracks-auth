using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public async Task<IActionResult> Index()
    {
      if (User.Identity.IsAuthenticated)
      {
        string userId = User.FindFirst("sub").Value;
        var userOrgs = await db.Users.Where(f => f.Id == userId).SelectMany(f => f.CustomOrganizations).Select(f => f.OrganizationId).ToListAsync();
        var apps = await db.Applications.Where(f => f.Organizations.Count == 0 || f.Organizations.Any(g => userOrgs.Contains(g.OrganizationId))).ToListAsync();
        ViewData["apps"] = apps;
        return View();
      }
      return View();
    }

    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Authorize]
    [HttpGet("/test/email/{to}")]
    public async Task<IActionResult> TestEmail(string to, [FromServices] IMessagingApi messaging)
    {
      await messaging.SendEmail(to, "Test email from auth service", "This is a test mail. I hope it made it through");
      return Content("OK");
    }
  }
}
