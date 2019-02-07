using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.Validation;
using Microsoft.EntityFrameworkCore;
using SarData.Auth.Data;

namespace SarData.Auth.Identity
{
  public class MultiOrganizationRequestValidator : ICustomAuthorizeRequestValidator
  {
    private readonly ApplicationDbContext db;
    private readonly ConfigurationDbContext configurationDb;

    public MultiOrganizationRequestValidator(ApplicationDbContext db, ConfigurationDbContext configurationDb)
    {
      this.db = db;
      this.configurationDb = configurationDb;
    }

    public async Task ValidateAsync(CustomAuthorizeRequestValidationContext context)
    {
      var user = context.Result.ValidatedRequest.Subject;

      if (user.Identity.IsAuthenticated)
      {
        if (!await UserCanAccessClient(user, context.Result.ValidatedRequest.ClientClaims, db))
        {
          context.Result.IsError = true;
          context.Result.Error = "app_denied";
          context.Result.ErrorDescription = $"Access to this application has been denied to {user.Identity.Name} ({user.FindFirst("email").Value}).";
        }
      }
      return;
    }

    public static async Task<bool> UserCanAccessClient(ClaimsPrincipal user, ICollection<Claim> clientClaims, ApplicationDbContext db)
    {
      var hasAccess = true;
      var applicationId = clientClaims.Where(f => f.Type == "application").SingleOrDefault()?.Value;
      if (!string.IsNullOrWhiteSpace(applicationId))
      {
        Guid appGuid = Guid.Parse(applicationId);
        var appOrgs = await db.Applications.Where(f => f.Id == appGuid).SelectMany(f => f.Organizations).Select(f => f.OrganizationId).ToListAsync();

        if (appOrgs.Count > 0)
        {
          var userId = user.FindFirst("sub").Value;
          hasAccess = await db.Users.Where(f => f.Id == userId).SelectMany(f => f.CustomOrganizations).AnyAsync(f => appOrgs.Contains(f.OrganizationId));
        }
      }

      return hasAccess;
    }
  }
}
