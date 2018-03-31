using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SarData.Auth.Models;
using SarData.Auth.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SarData.Auth
{
  public class LinkedMemberSigninManager : SignInManager<ApplicationUser>
  {
    private readonly IRemoteMembersService remoteMembers;

    public LinkedMemberSigninManager(
      IRemoteMembersService remoteMembers,
      LinkedMemberUserManager userManager,
      IHttpContextAccessor contextAccessor,
      IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
      IOptions<IdentityOptions> optionsAccessor,
      ILogger<LinkedMemberSigninManager> logger,
      IAuthenticationSchemeProvider schemes)
      : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes)
    {
      this.remoteMembers = remoteMembers;
    }

    public override async Task<bool> CanSignInAsync(ApplicationUser user)
    {
      bool can = await base.CanSignInAsync(user);
      return can;
    }

    protected override async Task<bool> IsLockedOut(ApplicationUser user)
    {
      bool locked = await base.IsLockedOut(user);
      if (!locked && user.MemberId != null)
      {
        RemoteMember member = await remoteMembers.GetMember(user.MemberId);
        locked = (member == null || !member.IsActive);
      }

      return locked;
    }

    protected override async Task<SignInResult> LockedOut(ApplicationUser user)
    {
      SignInResult result = await base.LockedOut(user);
      return result;
    }

    public override async Task<ClaimsPrincipal> CreateUserPrincipalAsync(ApplicationUser user)
    {
      ClaimsPrincipal principal = await base.CreateUserPrincipalAsync(user);
      return principal;
    }

    public override Task SignInAsync(ApplicationUser user, AuthenticationProperties authenticationProperties, string authenticationMethod = null)
    {
      return base.SignInAsync(user, authenticationProperties, authenticationMethod);
    }

    public override Task SignInAsync(ApplicationUser user, bool isPersistent, string authenticationMethod = null)
    {
      return base.SignInAsync(user, isPersistent, authenticationMethod);
    }
  }
}
