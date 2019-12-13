using Microsoft.AspNetCore.Identity;
using SarData.Auth.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SarData.Auth.Identity
{
  public class KnownUserConfirmation : IUserConfirmation<ApplicationUser>
  {
    public Task<bool> IsConfirmedAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
      throw new NotImplementedException();
    }
  }
}
