using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SarData.Auth.Data;
using SarData.Auth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Identity
{
  public class LinkedMemberUserManager : UserManager<ApplicationUser>
  {
    private readonly ApplicationDbContext db;

    public LinkedMemberUserManager(
      ApplicationDbContext db,
      IUserStore<ApplicationUser> store,
      IOptions<IdentityOptions> optionsAccessor,
      IPasswordHasher<ApplicationUser> passwordHasher,
      IEnumerable<IUserValidator<ApplicationUser>> userValidators,
      IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators,
      ILookupNormalizer keyNormalizer,
      IdentityErrorDescriber errors,
      IServiceProvider services,
      ILogger<LinkedMemberUserManager> logger
      )
      : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
      this.db = db;
    }

    public async Task<ApplicationUser> FindByMemberId(string memberId)
    {
      return await db.Users.Where(f => f.MemberId == memberId).OrderBy(f => f.Created).FirstOrDefaultAsync();
    }

    public override async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName)
    {
      var roles = db.Roles.Where(f => f.Name == roleName || f.Ancestors.Any(g => g.Parent.Name == roleName));
      var users = roles.SelectMany(f => f.UserMembers).Select(f => f.User).Distinct();

      return await users.ToListAsync();
    }
    
    public async Task<bool> UserIsInRole(string userId, string roleName)
    {
      var roles = db.Roles.Where(f => f.Name == roleName || f.Ancestors.Any(g => g.Parent.Name == roleName));
      var me = await roles.ToListAsync();
      var isMember = roles.SelectMany(f => f.UserMembers).AnyAsync(f => f.UserId == userId);
      return await isMember;
    }
  }
}
