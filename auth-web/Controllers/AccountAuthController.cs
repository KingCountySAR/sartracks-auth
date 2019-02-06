using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarData.Auth.Identity;
using System.Threading.Tasks;

namespace auth_web.Controllers
{
  [Authorize]
  [Route("Account/[action]")]
  public class AccountAuthController : Controller
  {
    private readonly LinkedMemberUserManager userManager;
    private readonly ApplicationRoleManager roleManager;

    public AccountAuthController(LinkedMemberUserManager userManager, ApplicationRoleManager roleManager)
    {
      this.userManager = userManager;
      this.roleManager = roleManager;
    }

    [HttpGet("/Account/{userId}/IsInGroup/{groupId}")]
    public async Task<IActionResult> IsInGroup(string userId, string groupId)
    {
      var data = new
      {
        Data = new
        {
          IsInGroup = await userManager.UserIsInRole(userId, groupId)
        }
      };
      return Ok(data);
    }
  }
}