using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SarData.Auth.Data;

namespace SarData.Auth.Identity
{
  public class LinkedMemberSigninManager : SignInManager<ApplicationUser>
  {
    public LinkedMemberSigninManager(
      ApplicationUserManager userManager,
      IHttpContextAccessor contextAccessor,
      IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
      IOptions<IdentityOptions> optionsAccessor,
      ILogger<LinkedMemberSigninManager> logger,
      IAuthenticationSchemeProvider schemes,
      IUserConfirmation<ApplicationUser> confirmation)
      : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override async Task<bool> CanSignInAsync(ApplicationUser user)
    {
      bool can = await base.CanSignInAsync(user);
      return can;
    }

    protected override async Task<bool> IsLockedOut(ApplicationUser user)
    {
      bool locked = await base.IsLockedOut(user);
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

    public override async Task SignInAsync(ApplicationUser user, AuthenticationProperties authenticationProperties, string authenticationMethod = null)
    {
      DateTimeOffset now = DateTimeOffset.UtcNow;
      if (!user.LastLogin.HasValue || (now - user.LastLogin.Value).TotalMinutes > 1)
      {
        user.LastLogin = now;
        await UserManager.UpdateAsync(user);
      }
      
      await base.SignInAsync(user, authenticationProperties, authenticationMethod);
    }

    public override Task SignInAsync(ApplicationUser user, bool isPersistent, string authenticationMethod = null)
    {
      return base.SignInAsync(user, isPersistent, authenticationMethod);
    }
  }
}
