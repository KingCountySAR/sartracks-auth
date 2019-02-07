using System;
using System.Linq;
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
        var applicationId = context.Result.ValidatedRequest.ClientClaims.Where(f => f.Type == "application").SingleOrDefault()?.Value;
        if (!string.IsNullOrWhiteSpace(applicationId)) {
          Guid appGuid = Guid.Parse(applicationId);
          var appOrgs = await db.Applications.Where(f => f.Id == appGuid).SelectMany(f => f.Organizations).Select(f => f.OrganizationId).ToListAsync();

          if (appOrgs.Count > 0)
          {
            var userId = user.FindFirst("sub").Value;
            var test = await db.Users.Where(f => f.Id == userId).SelectMany(f => f.CustomOrganizations).ToListAsync();
            var isInOrg = await db.Users.Where(f => f.Id == userId).SelectMany(f => f.CustomOrganizations).AnyAsync(f => appOrgs.Contains(f.OrganizationId));

            if (!isInOrg)
            {
              context.Result.IsError = true;
              context.Result.Error = "app_denied";
              context.Result.ErrorDescription = $"Access to this application has been denied to {user.Identity.Name} ({user.FindFirst("email").Value}).";
            }
          }
        }
      }
      return;
    }
  }
}
