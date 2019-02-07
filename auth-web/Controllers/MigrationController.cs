using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SarData.Auth.Data;
using SarData.Auth.Data.LegacyMigration;
using SarData.Auth.Identity;

namespace SarData.Auth.Controllers
{
  [Route("[controller]/[action]")]
  public class MigrationController : Controller
  {
    private readonly ApplicationUserManager users;
    private readonly LegacyAuthDbContext legacyDb;
    private readonly ApplicationRoleManager roleManager;

    public MigrationController(
        IConfiguration config,
        ApplicationUserManager userManager,
        ApplicationRoleManager roleManager,
        LegacyAuthDbContext legacyDb
      )
    {
      if (!bool.Parse(config["setupMode"] ?? "false")) throw new InvalidOperationException("Not in setup mode");
      users = userManager;
      this.roleManager = roleManager;
      this.legacyDb = legacyDb;
    }

    [HttpGet]
    public async Task<IActionResult> UserRoles()
    {
      var failures = new List<string>();
      var legacyAssignments = await legacyDb.AccountRoles.GroupBy(f => f.AccountRow_Id).Select(f => new { Account = f.Key, Roles = f.Select(g => g.Role.Name).ToArray() }).ToListAsync();

      foreach (var assignment in legacyAssignments)
      {
        await users.AddToRolesAsync(await users.FindByIdAsync(assignment.Account.ToString()), assignment.Roles);
      }

      return Content("Done");
    }

    [HttpGet]
    public async Task<IActionResult> blah()
    {
      await roleManager.RebuildInheritance();
      return Content("Done");
    }

    [HttpGet]
    public async Task<IActionResult> Roles()
    {
      var failures = new List<string>();
      TimeZoneInfo pst = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
      DateTime longAgo = new DateTime(1800, 1, 1);

      var legacyRoles = await legacyDb.Roles.ToArrayAsync();

      foreach (var role in legacyRoles)
      {
        await roleManager.CreateAsync(new ApplicationRole { Id = role.Id, Name = role.Name, Description = role.Description });
      }

      var roleRoles = legacyDb.RoleRoles.OrderBy(f => f.RoleRow_Id).ToArray();
      ApplicationRole child = new ApplicationRole();
      foreach (var row in roleRoles)
      {
        if (child == null || row.RoleRow_Id != child.Id)
        {
          if (child != null) await roleManager.UpdateAsync(child);
          child = await roleManager.FindByIdAsync(row.RoleRow_Id);
        }
        child.Ancestors.Add(new RoleRoleMembership
        {
          ParentId = row.RoleRow_Id1,
          Child = child,
          IsDirect = true
        });
      }

      // await roleManager.RebuildInheritance();

      return Content("Done\n\n" + string.Join("\n", failures));
    }


    [HttpGet]
    public async Task<IActionResult> AccountsLogins()
    {
      var failures = new List<string>();
      TimeZoneInfo pst = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
      DateTime longAgo = new DateTime(1800, 1, 1);

      foreach (var member in await legacyDb.Accounts.Include(f => f.Logins).ToListAsync())
      {
        string firstLoginProvider = member.Logins.OrderBy(f => f.Created).Select(f => f.Provider).FirstOrDefault();

        string nameSpacer = (string.IsNullOrEmpty(member.LastName) || string.IsNullOrEmpty(member.FirstName)) ? string.Empty : " ";
        var user = new ApplicationUser
        {
          Id = member.Id.ToString(),
          UserName = member.Username?.Replace(" ", ".") ?? $"@{member.Id}-{firstLoginProvider}",
          Email = member.Email,
          MemberId = member.MemberId.ToString(),
          PasswordHash = member.PasswordHash,
          Created = new DateTimeOffset(member.Created ?? longAgo, pst.GetUtcOffset(member.Created ?? longAgo))
        };
        var result = await users.CreateAsync(user);
        if (!result.Succeeded)
        {
          failures.AddRange(result.Errors.Select(f => f.Description));
          continue;
        }
        List<Claim> claims = new List<Claim> { new Claim(JwtClaimTypes.Name, $"{member.FirstName}{nameSpacer}{member.LastName}") };
        if (!string.IsNullOrEmpty(member.FirstName)) claims.Add(new Claim(JwtClaimTypes.GivenName, member.FirstName));
        if (!string.IsNullOrEmpty(member.LastName)) claims.Add(new Claim(JwtClaimTypes.FamilyName, member.LastName));
        if (!string.IsNullOrEmpty(member.Email)) claims.Add(new Claim(JwtClaimTypes.Email, member.Email));
        await users.AddClaimsAsync(user, claims);

        foreach (var login in member.Logins)
        {
          await users.AddLoginAsync(user, new Microsoft.AspNetCore.Identity.UserLoginInfo(login.Provider, login.ProviderId, login.Provider));
        }

        if (member.Locked.HasValue)
        {
          await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(200));
        }
      }

      return Content("Done\n\n" + string.Join("\n", failures));
    }
  }
}
