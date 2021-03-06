﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SarData.Auth.Data;
using SarData.Auth.Models.ManageViewModels;
using SarData.Auth.Services;
using SarData.Common.Apis.Messaging;

namespace SarData.Auth.Controllers
{
  [Authorize]
  [Route("[controller]/[action]")]
  public class ManageController : Controller
  {
    private readonly UserManager<ApplicationUser> users;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IMessagingApi _emailSender;
    private readonly ILogger _logger;
    private readonly UrlEncoder _urlEncoder;

    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
    private const string RecoveryCodesKey = nameof(RecoveryCodesKey);

    public ManageController(
      UserManager<ApplicationUser> userManager,
      SignInManager<ApplicationUser> signInManager,
      IMessagingApi emailSender,
      ILogger<ManageController> logger,
      UrlEncoder urlEncoder)
    {
      users = userManager;
      _signInManager = signInManager;
      _emailSender = emailSender;
      _logger = logger;
      _urlEncoder = urlEncoder;
    }

    [TempData]
    public string StatusMessage { get; set; }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var model = new IndexViewModel
      {
        Username = user.UserName.StartsWith("@") ? string.Empty : user.UserName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        IsEmailConfirmed = user.EmailConfirmed,
        StatusMessage = StatusMessage,
        IsMember = !string.IsNullOrEmpty(user.MemberId)
      };

      return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IndexViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }

      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }
      model.IsMember = user.IsMember;

      IdentityResult result;
      if (user.UserName.StartsWith("@") && !string.IsNullOrEmpty(user.UserName))
      {
        var otherUser = await users.FindByNameAsync(model.Username);
        if (otherUser != null)
        {
          ModelState.AddModelError(nameof(model.Username), $"Username '{model.Username}' already in use");
          model.Username = string.Empty;
        }
        else
        {
          result = await users.SetUserNameAsync(user, model.Username);
          if (!result.Succeeded)
          {
            throw new ApplicationException($"Unexpected error occured settings username for user with ID '{user.Id}.");
          }
        }
      }

      if (!user.IsMember)
      {
        var email = user.Email;
        if (model.Email != email)
        {
          var setEmailResult = await users.SetEmailAsync(user, model.Email);
          if (!setEmailResult.Succeeded)
          {
            throw new ApplicationException($"Unexpected error occurred setting email for user with ID '{user.Id}'.");
          }
        }

        var phoneNumber = user.PhoneNumber;
        if (model.PhoneNumber != phoneNumber)
        {
          var setPhoneResult = await users.SetPhoneNumberAsync(user, model.PhoneNumber);
          if (!setPhoneResult.Succeeded)
          {
            throw new ApplicationException($"Unexpected error occurred setting phone number for user with ID '{user.Id}'.");
          }
        }
      }

      if (ModelState.IsValid)
      {
        StatusMessage = "Your profile has been updated";
        return RedirectToAction(nameof(Index));
      }
      return View(nameof(Index), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendVerificationEmail(IndexViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }

      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var code = await users.GenerateEmailConfirmationTokenAsync(user);
      var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
      var email = user.Email;
      await _emailSender.SendEmailConfirmationAsync(email, callbackUrl);

      StatusMessage = "Verification email sent. Please check your email.";
      return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var hasPassword = await users.HasPasswordAsync(user);
      if (!hasPassword)
      {
        return RedirectToAction(nameof(SetPassword));
      }

      var model = new ChangePasswordViewModel { StatusMessage = StatusMessage };
      return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }

      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var changePasswordResult = await users.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
      if (!changePasswordResult.Succeeded)
      {
        AddErrors(changePasswordResult);
        return View(model);
      }

      await _signInManager.SignInAsync(user, isPersistent: false);
      _logger.LogInformation("User changed their password successfully.");
      StatusMessage = "Your password has been changed.";

      return RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    public async Task<IActionResult> SetPassword()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var hasPassword = await users.HasPasswordAsync(user);

      if (hasPassword)
      {
        return RedirectToAction(nameof(ChangePassword));
      }

      var model = new SetPasswordViewModel { StatusMessage = StatusMessage };
      return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }

      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var addPasswordResult = await users.AddPasswordAsync(user, model.NewPassword);
      if (!addPasswordResult.Succeeded)
      {
        AddErrors(addPasswordResult);
        return View(model);
      }

      await _signInManager.SignInAsync(user, isPersistent: false);
      StatusMessage = "Your password has been set.";

      return RedirectToAction(nameof(SetPassword));
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLogins()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var model = new ExternalLoginsViewModel { CurrentLogins = await users.GetLoginsAsync(user) };
      model.OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
          .ToList();
      model.ShowRemoveButton = await users.HasPasswordAsync(user) || model.CurrentLogins.Count > 1;
      model.StatusMessage = StatusMessage;

      return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkLogin(string provider)
    {
      // Clear the existing external cookie to ensure a clean login process
      await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

      // Request a redirect to the external login provider to link a login for the current user
      var redirectUrl = Url.Action(nameof(LinkLoginCallback));
      var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, users.GetUserId(User));
      return new ChallengeResult(provider, properties);
    }

    [HttpGet]
    public async Task<IActionResult> LinkLoginCallback()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
      if (info == null)
      {
        throw new ApplicationException($"Unexpected error occurred loading external login info for user with ID '{user.Id}'.");
      }

      info.ProviderDisplayName += " - " + (info.Principal.FindFirstValue(ClaimTypes.Email) ?? "unknown");
      var result = await users.AddLoginAsync(user, info);

      if (result.Succeeded)
      {
        StatusMessage = "The external login was added.";
      }
      else if (result.Errors.Any(f => f.Code.Equals("LoginAlreadyAssociated", StringComparison.OrdinalIgnoreCase)))
      {
        StatusMessage = "Warning: The external login was already linked - no changes were made.";
      }
      else
      {
        throw new ApplicationException($"Unexpected error occurred adding external login for user with ID '{user.Id}'.");
      }

      // Clear the existing external cookie to ensure a clean login process
      await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

      return RedirectToAction(nameof(ExternalLogins));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel model)
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var result = await users.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey);
      if (!result.Succeeded)
      {
        throw new ApplicationException($"Unexpected error occurred removing external login for user with ID '{user.Id}'.");
      }

      await _signInManager.SignInAsync(user, isPersistent: false);
      StatusMessage = "The external login was removed.";
      return RedirectToAction(nameof(ExternalLogins));
    }

    [HttpGet]
    public async Task<IActionResult> TwoFactorAuthentication()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var model = new TwoFactorAuthenticationViewModel
      {
        HasAuthenticator = await users.GetAuthenticatorKeyAsync(user) != null,
        Is2faEnabled = user.TwoFactorEnabled,
        RecoveryCodesLeft = await users.CountRecoveryCodesAsync(user),
      };

      return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Disable2faWarning()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      if (!user.TwoFactorEnabled)
      {
        throw new ApplicationException($"Unexpected error occured disabling 2FA for user with ID '{user.Id}'.");
      }

      return View(nameof(Disable2fa));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable2fa()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var disable2faResult = await users.SetTwoFactorEnabledAsync(user, false);
      if (!disable2faResult.Succeeded)
      {
        throw new ApplicationException($"Unexpected error occured disabling 2FA for user with ID '{user.Id}'.");
      }

      _logger.LogInformation("User with ID {UserId} has disabled 2fa.", user.Id);
      return RedirectToAction(nameof(TwoFactorAuthentication));
    }

    [HttpGet]
    public async Task<IActionResult> EnableAuthenticator()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      var model = new EnableAuthenticatorViewModel();
      await LoadSharedKeyAndQrCodeUriAsync(user, model);

      return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      if (!ModelState.IsValid)
      {
        await LoadSharedKeyAndQrCodeUriAsync(user, model);
        return View(model);
      }

      // Strip spaces and hypens
      var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

      var is2faTokenValid = await users.VerifyTwoFactorTokenAsync(
          user, users.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

      if (!is2faTokenValid)
      {
        ModelState.AddModelError("Code", "Verification code is invalid.");
        await LoadSharedKeyAndQrCodeUriAsync(user, model);
        return View(model);
      }

      await users.SetTwoFactorEnabledAsync(user, true);
      _logger.LogInformation("User with ID {UserId} has enabled 2FA with an authenticator app.", user.Id);
      var recoveryCodes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
      TempData[RecoveryCodesKey] = recoveryCodes.ToArray();

      return RedirectToAction(nameof(ShowRecoveryCodes));
    }

    [HttpGet]
    public IActionResult ShowRecoveryCodes()
    {
      var recoveryCodes = (string[])TempData[RecoveryCodesKey];
      if (recoveryCodes == null)
      {
        return RedirectToAction(nameof(TwoFactorAuthentication));
      }

      var model = new ShowRecoveryCodesViewModel { RecoveryCodes = recoveryCodes };
      return View(model);
    }

    [HttpGet]
    public IActionResult ResetAuthenticatorWarning()
    {
      return View(nameof(ResetAuthenticator));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAuthenticator()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      await users.SetTwoFactorEnabledAsync(user, false);
      await users.ResetAuthenticatorKeyAsync(user);
      _logger.LogInformation("User with id '{UserId}' has reset their authentication app key.", user.Id);

      return RedirectToAction(nameof(EnableAuthenticator));
    }

    [HttpGet]
    public async Task<IActionResult> GenerateRecoveryCodesWarning()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      if (!user.TwoFactorEnabled)
      {
        throw new ApplicationException($"Cannot generate recovery codes for user with ID '{user.Id}' because they do not have 2FA enabled.");
      }

      return View(nameof(GenerateRecoveryCodes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
      var user = await users.GetUserAsync(User);
      if (user == null)
      {
        throw new ApplicationException($"Unable to load user with ID '{users.GetUserId(User)}'.");
      }

      if (!user.TwoFactorEnabled)
      {
        throw new ApplicationException($"Cannot generate recovery codes for user with ID '{user.Id}' as they do not have 2FA enabled.");
      }

      var recoveryCodes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
      _logger.LogInformation("User with ID {UserId} has generated new 2FA recovery codes.", user.Id);

      var model = new ShowRecoveryCodesViewModel { RecoveryCodes = recoveryCodes.ToArray() };

      return View(nameof(ShowRecoveryCodes), model);
    }

    #region Helpers

    private void AddErrors(IdentityResult result)
    {
      foreach (var error in result.Errors)
      {
        ModelState.AddModelError(string.Empty, error.Description);
      }
    }

    private string FormatKey(string unformattedKey)
    {
      var result = new StringBuilder();
      int currentPosition = 0;
      while (currentPosition + 4 < unformattedKey.Length)
      {
        result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
        currentPosition += 4;
      }
      if (currentPosition < unformattedKey.Length)
      {
        result.Append(unformattedKey.Substring(currentPosition));
      }

      return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
      return string.Format(
          AuthenticatorUriFormat,
          _urlEncoder.Encode("SAR Tracks"),
          _urlEncoder.Encode(email),
          unformattedKey);
    }

    private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user, EnableAuthenticatorViewModel model)
    {
      var unformattedKey = await users.GetAuthenticatorKeyAsync(user);
      if (string.IsNullOrEmpty(unformattedKey))
      {
        await users.ResetAuthenticatorKeyAsync(user);
        unformattedKey = await users.GetAuthenticatorKeyAsync(user);
      }

      model.SharedKey = FormatKey(unformattedKey);
      model.AuthenticatorUri = GenerateQrCodeUri(user.Email, unformattedKey);
    }

    #endregion
  }
}
