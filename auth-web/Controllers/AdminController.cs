using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace auth_web.Controllers
{
  [Authorize]
  public class AdminController : Controller
  {
    [HttpGet("/admin/accounts")]
    public ActionResult Accounts()
    {
      return View();
    }
  }
}