using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SarData.Auth.Data;
using SarData.Auth.Identity;
using SarData.Auth.Models;
using SarData.Auth.Models.AccountViewModels;
using SarData.Auth.Services;
using SarData.Common.Apis.Messaging;

namespace SarData.Auth.Controllers
{
  [Authorize]
  [Route("[action]")]
  public class AccountController : Controller
  {
    private readonly IRemoteMembersService remoteMembers;
    private readonly ApplicationDbContext db;
    private readonly ApplicationUserManager users;
    private readonly SignInManager<ApplicationUser> signin;
    private readonly IIdentityServerInteractionService interaction;
    private readonly IMessagingApi messaging;
    private readonly ILogger logger;

    public AccountController(
        IRemoteMembersService remoteMembers,
        ApplicationDbContext db,
        ApplicationUserManager userManager,
        SignInManager<ApplicationUser> signInManager,
        IIdentityServerInteractionService interaction,
        IMessagingApi emailSender,
        ILogger<AccountController> logger)
    {
      this.remoteMembers = remoteMembers;
      this.db = db;
      users = userManager;
      signin = signInManager;
      this.interaction = interaction;
      messaging = emailSender;
      this.logger = logger;
    }

    [TempData]
    public string ErrorMessage { get; set; }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string returnUrl = null)
    {
      // Clear the existing external cookie to ensure a clean login process
      await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

      ViewData["ReturnUrl"] = returnUrl;
      return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
      ViewData["ReturnUrl"] = returnUrl;
      if (ModelState.IsValid)
      {
        // This doesn't count login failures towards account lockout
        // To enable password failures to trigger account lockout, set lockoutOnFailure: true
        var result = await signin.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
          logger.LogInformation("User logged in.");
          return RedirectToLocal(returnUrl);
        }
        if (result.RequiresTwoFactor)
        {
          return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
        }
        if (result.IsLockedOut)
        {
          logger.LogWarning("User account locked out.");
          return RedirectToAction(nameof(Lockout));
        }
        else
        {
          ModelState.AddModelError(string.Empty, "Invalid login attempt.");
          return View(model);
        }
      }

      // If we got this far, something failed, redisplay form
      return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> LoginError([FromQuery] string errorId)
    {
      var errorContext = await interaction.GetErrorContextAsync(errorId);
      ViewData["errorMessage"] = errorContext.ErrorDescription;
      return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
    {
      // Ensure the user has gone through the username & password screen first
      var user = await signin.GetTwoFactorAuthenticationUserAsync();

      if (user == null)
      {
        throw new ApplicationException($"Unable to load two-factor authentication user.");
      }

      var model = new LoginWith2faViewModel { RememberMe = rememberMe };
      ViewData["ReturnUrl"] = returnUrl;

      return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }

      var user = await signin.GetTwoFactorAuthenticationUserAsync();
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

      var result = await signin.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

      if (result.Succeeded)
      {
        logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
        return RedirectToLocal(returnUrl);
      }
      else if (result.IsLockedOut)
      {
        logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
        return RedirectToAction(nameof(Lockout));
      }
      else
      {
        logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
        ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
        return View();
      }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
    {
      // Ensure the user has gone through the username & password screen first
      var user = await signin.GetTwoFactorAuthenticationUserAsync();
      if (user == null)
      {
        throw new ApplicationException($"Unable to load two-factor authentication user.");
      }

      ViewData["ReturnUrl"] = returnUrl;

      return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = null)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }

      var user = await signin.GetTwoFactorAuthenticationUserAsync();
      if (user == null)
      {
        throw new ApplicationException($"Unable to load two-factor authentication user.");
      }

      var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

      var result = await signin.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

      if (result.Succeeded)
      {
        logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
        return RedirectToLocal(returnUrl);
      }
      if (result.IsLockedOut)
      {
        logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
        return RedirectToAction(nameof(Lockout));
      }
      else
      {
        logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
        ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
        return View();
      }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lockout()
    {
      return View();
    }

    [HttpGet("/Account/Logout")]
    public ActionResult StartLogout()
    {
      return View("Logout");
    }

    [HttpPost("/Account/Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
      await signin.SignOutAsync();
      logger.LogInformation("User logged out.");
      return Redirect("/");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string returnUrl = null)
    {
      // Request a redirect to the external login provider.
      var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
      var properties = signin.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
      return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
    {
      if (remoteError != null)
      {
        ErrorMessage = $"Error from external provider: {remoteError}";
        return RedirectToAction(nameof(Login));
      }
      var info = await signin.GetExternalLoginInfoAsync();
      if (info == null)
      {
        return RedirectToAction(nameof(Login));
      }

      // Sign in the user with this external login provider if the user already has a login.
      var result = await signin.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
      if (result.Succeeded)
      {
        logger.LogInformation("User {Name} logged in with {Provider} provider.", User.FindFirstValue(ClaimTypes.Name), info.LoginProvider);
        return RedirectToLocal(returnUrl);
      }
      if (result.IsLockedOut)
      {
        logger.LogInformation("User {Name} is locked out.", User.FindFirstValue(ClaimTypes.Name));
        return RedirectToAction(nameof(Lockout));
      }
      else
      {
        // If the user does not have an account, then ask the user to create an account.
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["LoginProvider"] = info.LoginProvider;
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        return View("ExternalLogin", new ExternalLoginViewModel { Email = email });
      }
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null)
    {
      ViewData["ReturnUrl"] = returnUrl;
      if (ModelState.IsValid)
      {
        // Get the information about the user from the external login provider
        var info = await signin.GetExternalLoginInfoAsync();
        if (info == null)
        {
          throw new ApplicationException("Error loading external login information during confirmation.");
        }

        ViewData["LoginProvider"] = info.LoginProvider;

        IdentityResult result;
        List<RemoteMember> members = new List<RemoteMember>();
        bool foundPhone = false;
        if (!string.IsNullOrWhiteSpace(model.Phone))
        {
          members = await remoteMembers.FindByPhone(model.Phone);
        }
        if (members.Count == 1)
        {
          foundPhone = true;
        }
        else if (!string.IsNullOrWhiteSpace(model.Email))
        {
          members = await remoteMembers.FindByEmail(model.Email);
        }

        if (members.Count != 1)
        {
          result = IdentityResult.Failed(new IdentityError { Code = "NotMemberEmail", Description = "Unable to find member." });
        }
        else
        {
          var codeRow = await db.ExternalLoginCodes.FindAsync(info.ProviderKey);
          if (codeRow == null)
          {
            codeRow = new ExternalLoginCode { LoginSubject = info.ProviderKey };
            db.ExternalLoginCodes.Add(codeRow);
          }
          codeRow.MemberId = members[0].Id;
          codeRow.ExpiresUtc = DateTime.UtcNow.AddMinutes(10);
          codeRow.Code = new Random().Next(1000000).ToString("000000");
          await db.SaveChangesAsync();

          if (foundPhone)
          {
            await messaging.SendText(model.Phone, "Your verification code: " + codeRow.Code);
          }
          else
          {
            await messaging.SendEmail(model.Email, "Verification Code", "Your verificaton code: " + codeRow.Code);
          }

          return View("ExternalVerify");
        }
        AddErrors(result);
      }

      return View(nameof(ExternalLogin), model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalVerifyMembership(ExternalVerifyViewModel model, string returnUrl = null)
    {
      if (ModelState.IsValid)
      {
        // Get the information about the user from the external login provider
        var info = await signin.GetExternalLoginInfoAsync();
        if (info == null)
        {
          throw new ApplicationException("Error loading external login information during confirmation.");
        }

        var codeRow = await db.ExternalLoginCodes.FindAsync(info.ProviderKey);
        bool success = await VerifyMembership(codeRow, info, model);
        if (success)
        {
          return RedirectToLocal(returnUrl);
        }
      }
      return View("ExternalVerify");
    }

    private async Task<bool> VerifyMembership(ExternalLoginCode codeRow, ExternalLoginInfo info, ExternalVerifyViewModel model)
    {
      if (codeRow == null)
      {
        AddErrors(IdentityResult.Failed(new IdentityError { Code = "NoMembershipCode", Description = "Invalid or unknown code." }));
        return false;
      }

      if (string.Compare(codeRow.Code, model.Code, true) != 0)
      {
        AddErrors(IdentityResult.Failed(new IdentityError { Code = "MembershipCodeInvalid", Description = "Invalid or unknown code." }));
        return false;
      }

      if (codeRow.ExpiresUtc < DateTime.UtcNow)
      {
        AddErrors(IdentityResult.Failed(new IdentityError { Code = "MembershipCodeExpired", Description = "Code is expired." }));
        return false;
      }

      var member = await remoteMembers.GetMember(codeRow.MemberId);
      if (member == null)
      {
        AddErrors(IdentityResult.Failed(new IdentityError { Code = "MembershipCodeInvalid", Description = "Invalid or unknown code." }));
        return false;
      }

      IdentityResult result;
      var user = await users.FindByMemberId(member.Id);
      if (user == null)
      {
        string nameSpacer = (string.IsNullOrEmpty(member.LastName) || string.IsNullOrEmpty(member.FirstName)) ? string.Empty : " ";
        user = new ApplicationUser
        {
          UserName = $"@{member.Id}-{info.LoginProvider}",
          Email = member.PrimaryEmail,
          MemberId = member.Id,
          PhoneNumber = member.PrimaryPhone,
          FirstName = member.FirstName,
          LastName = member.LastName,
          Created = DateTimeOffset.UtcNow
        };
        result = await users.CreateAsync(user);
        if (!result.Succeeded)
        {
          AddErrors(result);
          return false;
        }

        logger.LogInformation($"User created for {member.FirstName} {member.LastName}. Will link to {info.ProviderDisplayName} login.");
      }

      info.ProviderDisplayName += " - " + (info.Principal.FindFirstValue(ClaimTypes.Email) ?? "unknown");
      result = await users.AddLoginAsync(user, info);
      if (!result.Succeeded)
      {
        AddErrors(result);
        return false;
      }

      await signin.SignInAsync(user, isPersistent: false);
      logger.LogInformation($"Associated {info.ProviderDisplayName} login with account {user.Id} ({user.UserName} {user.Email})");
      return true;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
      if (userId == null || code == null)
      {
        return Redirect("/");
      }
      var user = await users.FindByIdAsync(userId);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{userId}'.");
      }
      var result = await users.ConfirmEmailAsync(user, code);
      return View(result.Succeeded ? "ConfirmEmail" : "Error");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(string username)
    {
      if (string.IsNullOrWhiteSpace(username)) {
        return View();
      }
      else
      {
        return await ForgotPassword(new ForgotPasswordViewModel { Username = username });
      }
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
      if (ModelState.IsValid)
      {
        ApplicationUser user;
        if (!string.IsNullOrWhiteSpace(model.Email)) {
          user = await users.FindByEmailAsync(model.Email);
          if (user != null && !(await users.IsEmailConfirmedAsync(user))) user = null;
        }
        else
        {
          user = await users.FindByNameAsync(model.Username);
        }

        if (user != null)
        {
          // For more information on how to enable account confirmation and password reset please
          // visit https://go.microsoft.com/fwlink/?LinkID=532713
          var code = await users.GeneratePasswordResetTokenAsync(user);
          var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
          await messaging.SendEmail(user.Email, "Reset Password",
            $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a><br/><br/>Sent to email address {user.Email}.");
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
      }

      // If we got this far, something failed, redisplay form
      return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
      return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string code = null)
    {
      if (code == null)
      {
        throw new ApplicationException("A code must be supplied for password reset.");
      }
      var model = new ResetPasswordViewModel { Code = code };
      return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }
      var user = await users.FindByEmailAsync(model.Email);
      if (user == null)
      {
        // Don't reveal that the user does not exist
        return RedirectToAction(nameof(ResetPasswordConfirmation));
      }
      var result = await users.ResetPasswordAsync(user, model.Code, model.Password);
      if (result.Succeeded)
      {
        return RedirectToAction(nameof(ResetPasswordConfirmation));
      }
      AddErrors(result);
      return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
      return View();
    }


    [HttpGet]
    public IActionResult AccessDenied()
    {
      return View();
    }

    #region Helpers

    private void AddErrors(IdentityResult result)
    {
      foreach (var error in result.Errors)
      {
        ModelState.AddModelError(string.Empty, error.Description);
      }
    }

    private IActionResult RedirectToLocal(string returnUrl)
    {
      if (Url.IsLocalUrl(returnUrl))
      {
        return Redirect(returnUrl);
      }
      else
      {
        return Redirect("/");
      }
    }

    #endregion
  }
}
