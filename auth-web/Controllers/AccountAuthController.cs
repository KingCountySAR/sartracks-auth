using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarData.Auth.Identity;
using System.Threading.Tasks;

namespace auth_web.Controllers
{
  [Authorize(AuthenticationSchemes = AuthSchemes)]
  [Route("Account/[action]")]
  public class AccountAuthController : Controller
  {
    private const string AuthSchemes = JwtBearerDefaults.AuthenticationScheme;

    private readonly ApplicationUserManager userManager;
    private readonly ApplicationRoleManager roleManager;

    public AccountAuthController(ApplicationUserManager userManager, ApplicationRoleManager roleManager)
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

    [HttpGet("/Account/{userId}/Groups")]
    public async Task<IActionResult> GetGroups(string userId, [FromQuery] bool? direct = false)
    {
      var data = new
      {
        Data = await userManager.UsersRoles(userId, direct)
      };

      return Ok(data);
    }
  }
}