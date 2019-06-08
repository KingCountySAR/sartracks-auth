using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.AspNetIdentity;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SarData.Auth.Data;

namespace SarData.Auth.Identity
{
  public class MemberProfileService : ProfileService<ApplicationUser>
  {
    public MemberProfileService(UserManager<ApplicationUser> userManager, IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory, ILogger<ProfileService<ApplicationUser>> logger)
      : base(userManager, claimsFactory, logger)
    {
    }

    public override async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
      await base.GetProfileDataAsync(context);
      var user = await UserManager.FindByIdAsync(context.Subject.FindFirstValue("sub"));
      if (user.IsMember)
      {
        context.IssuedClaims.Add(new Claim("memberId", user.MemberId));
      }
    }
  }
}
