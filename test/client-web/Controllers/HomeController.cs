using System.Diagnostics;
using Internal.SarData.Client.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internal.SarData.Client.Web.Controllers
{
  public class HomeController : Controller
  {
    [Authorize]
    public IActionResult Index()
    {
      return View();
    }

    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
  }
}
