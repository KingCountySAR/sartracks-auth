using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
//using ComponentSpace.Saml2;
//using ComponentSpace.Saml2.Assertions;
//using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SarData.Auth.Data;
using SarData.Auth.Identity;
using SarData.Auth.Saml;

namespace SarData.Auth.Controllers
{
  [Route("[controller]/[action]")]
  public class SamlController : Controller
  {
    private readonly SamlImplementation samlProvider;
    private readonly ConfigurationDbContext configDb;
    private readonly ApplicationDbContext db;

    public SamlController(
      SamlImplementation samlProvider,
      ConfigurationDbContext configDb,
      ApplicationDbContext db
    )
    {
      this.samlProvider = samlProvider;
      this.configDb = configDb;
      this.db = db;
    }

    [HttpGet]
    public async Task<IActionResult> SingleSignOnService()
    {
      await samlProvider.ReceiveSsoAsync();

      if (!User.Identity.IsAuthenticated)
      {
        return RedirectToAction("SingleSignOnServiceCompletion");
      }

      return await FinishUp();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> SingleSignOnServiceCompletion()
    {
      return await FinishUp();
    }

    private async Task<IActionResult> FinishUp()
    {
      var partner = await samlProvider.GetPendingPartner();
      if (string.IsNullOrWhiteSpace(partner))
      {
        return Content("Login Error");
      }

      var clientClaims = (await configDb.Clients.Where(f => f.ClientId == partner).SelectMany(f => f.Claims).ToListAsync()).Select(f => new Claim(f.Type, f.Value)).ToList();
      var canAccess = await MultiOrganizationRequestValidator.UserCanAccessClient(User, clientClaims, db);
      if (!canAccess)
      {
        return Content("Access to application is denied");
      }

      await samlProvider.CompleteSsoAsync(User);
      return new EmptyResult();
    }
  }
}