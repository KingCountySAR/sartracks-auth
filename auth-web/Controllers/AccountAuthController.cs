using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SarData.Auth.Data;
using SarData.Auth.Identity;
using SarData.Auth.Models.AccountViewModels;

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

    [HttpPost("/Account")]
    public async Task<IActionResult> CreateAccount([FromBody, BindRequired] CreateAccountViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(new { Error = "Invalid request" });
      }

      if (! await userManager.UserIsInRole(User.Claims.First(f => f.Type == ClaimTypes.NameIdentifier).Value, "acct-managers"))
      {
        return Forbid();
      }

      var user = new ApplicationUser {
        UserName = model.Username,
        Email = model.Email,
        EmailConfirmed = true,
        MemberId = model.MemberId,
        LockoutEnabled = true,
        Created = DateTimeOffset.Now
      };

      var result = await userManager.CreateAsync(user);

      await userManager.AddClaimAsync(user, new Claim("name", model.Name));

      return Ok(new { Data = new AccountInfo {
        Id = user.Id,
        Username = user.UserName,
        Email = user.Email,
        Name = (await userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "name")?.Value
      } });
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

    [HttpGet("/Account/checkname/{username}")]
    public async Task<IActionResult> CheckName(string username)
    {
      return Ok(new
      {
        Data = new
        {
          Available = (await userManager.Users.CountAsync(f => f.UserName == username)) == 0
        }
      });
    }

    [HttpGet("/Account/byname/{username}")]
    public async Task<IActionResult> ByName(string username)
    {
      var user = await userManager.Users.Where(f => f.UserName == username).FirstOrDefaultAsync();
      if (user == null) return NotFound(new { Username = username });

      var claims = await userManager.GetClaimsAsync(user);

      var data = new
      {
        Data = new
        {
          user.Id,
          user.MemberId,
          Name = claims.Where(f => f.Type == ClaimTypes.Name)
        }
      };

      return Ok(data);
    }

    [HttpGet("/Account/ForMember/{memberId}")]
    public async Task<IActionResult> ForMember(string memberId)
    {
      var data = new List<AccountInfo>();
      var users = await userManager.Users.Where(f => f.MemberId == memberId).ToListAsync();
      foreach (var user in users)
      {
        data.Add(new AccountInfo
        {
          Id = user.Id,
          Username = user.UserName,
          Email = user.Email,
          Name = (await userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "name")?.Value
        });
      }

      return Ok(new
      {
        Data = data
      });
    }

    class AccountInfo
    {
      public string Id { get; set; }
      public string Username { get; set; }
      public string Email { get; set; }
      public string Name { get; set; }
    }
  }
}