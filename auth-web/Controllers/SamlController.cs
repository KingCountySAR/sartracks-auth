//using System.Collections.Generic;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using ComponentSpace.Saml2;
//using ComponentSpace.Saml2.Assertions;
//using IdentityServer4.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace SarData.Auth.Controllers
//{
//  [Route("[controller]/[action]")]
//  public class SamlController : Controller
//  {
//    private readonly ISamlIdentityProvider samlIdentityProvider;
//    private readonly IIdentityServerInteractionService identityServerInteractionService;

//    public SamlController(
//      ISamlIdentityProvider samlIdentityProvider,
//      IIdentityServerInteractionService identityServerInteractionService
//    ) {
//      this.samlIdentityProvider = samlIdentityProvider;
//      this.identityServerInteractionService = identityServerInteractionService;
//    }

//    [HttpGet]
//    public async Task<IActionResult> SingleSignOnService()
//    {
//      await samlIdentityProvider.ReceiveSsoAsync();

//      if (User.Identity.IsAuthenticated)
//      {
//        await CompleteSsoAsync();
//        return new EmptyResult();
//      }
//      else
//      {
//        return RedirectToAction("SingleSignOnServiceCompletion");
//      }
//    }

//    [HttpGet]
//    [Authorize]
//    public async Task<ActionResult> SingleSignOnServiceCompletion()
//    {
//      await CompleteSsoAsync();
//      return new EmptyResult();
//    }

//    private Task CompleteSsoAsync()
//    {
//      // Get the name of the logged in user.
//      var userName = User.Identity.Name;
//      // Include claims as SAML attributes.
//      var attributes = new List<SamlAttribute>();
//      string email = userName;
//      foreach (var claim in ((ClaimsIdentity)User.Identity).Claims)
//      {
//        if (claim.Type == "email") userName = claim.Value;
//      }
//      // The user is logged in at the identity provider.
//      // Respond to the authn request by sending a SAML response containing a SAML assertion to the SP.
//      return samlIdentityProvider.SendSsoAsync(userName, attributes);
//    }
//  }
//}