using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SarData.Auth.Data;
using SarData.Auth.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SarData.Auth
{
  public class LinkedMemberUserManager : UserManager<ApplicationUser>
  {
    private readonly ApplicationDbContext db;

    public LinkedMemberUserManager(ApplicationDbContext db, IUserStore<ApplicationUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<ApplicationUser> passwordHasher, IEnumerable<IUserValidator<ApplicationUser>> userValidators, IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<LinkedMemberUserManager> logger)
      : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
      this.db = db;
    }

    public async Task<ApplicationUser> FindByMemberId(string memberId)
    {
      return await db.Users.SingleOrDefaultAsync(f => f.MemberId == memberId);
    }

   
  }
}
