using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SailScores.Web.Extensions;
using SailScores.Web.Models.AccountViewModels;
using SailScores.Web.Services;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SailScores.Identity.Entities;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

// this keeps the controller out of swagger.
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
[Route("[controller]/[action]")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly IAuthorizationService _authService;
    private readonly ILogger _logger;
    private readonly ITurnstileService _turnstileService;
    private readonly AppSettingsService _appSettingsService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        IAuthorizationService authService,
        ILogger<AccountController> logger,
        ITurnstileService turnstileService,
        AppSettingsService appSettingsService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _authService = authService;
        _logger = logger;
        _turnstileService = turnstileService;
        _appSettingsService = appSettingsService;
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
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                var user = await _userManager.FindByEmailAsync(model.Email);
                await UpdateLastSuccessfulLoginAsync(user);
                string homeClub = await _authService.GetHomeClub(model.Email);

                //if they have a home club and return url isn't in it
                // and isn't to club request page
                // redirect to home club front page.
                if (!String.IsNullOrWhiteSpace(homeClub) && (
                    String.IsNullOrWhiteSpace(returnUrl) ||
                    (!returnUrl.Contains($"/{homeClub}", StringComparison.InvariantCultureIgnoreCase) &&
                     !returnUrl.Contains("clubrequest", StringComparison.InvariantCultureIgnoreCase))))
                {
                    return RedirectToAction(
                        controllerName: "Club",
                        actionName: "Index",
                        routeValues: new {ClubInitials = homeClub});
                }
                return Redirect(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
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
    [AllowAnonymous]
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
    {
        // Ensure the user has gone through the username & password screen first
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

        if (user == null)
        {
            _logger.LogWarning("Two-factor authentication attempted without prior authentication");
            return RedirectToAction(nameof(Login));
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

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            _logger.LogWarning("Two-factor authentication POST attempted without prior authentication");
            return RedirectToAction(nameof(Login));
        }

        var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

        if (result.Succeeded)
        {
            _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
            await UpdateLastSuccessfulLoginAsync(user);
            return RedirectToLocal(returnUrl);
        }
        else if (result.IsLockedOut)
        {
            _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
            return RedirectToAction(nameof(Lockout));
        }
        else
        {
            _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
            ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
            return View();
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
    {
        // Ensure the user has gone through the username & password screen first
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            _logger.LogWarning("Recovery code login attempted without prior authentication");
            return RedirectToAction(nameof(Login));
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

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            _logger.LogWarning("Recovery code login POST attempted without prior authentication");
            return RedirectToAction(nameof(Login));
        }

        var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        if (result.Succeeded)
        {
            _logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
            await UpdateLastSuccessfulLoginAsync(user);
            return RedirectToLocal(returnUrl);
        }
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
            return RedirectToAction(nameof(Lockout));
        }
        else
        {
            _logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await IsTurnstileValidAsync())
        {
            ModelState.AddModelError(string.Empty, "Please complete the captcha challenge.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            EnableAppInsights = model.EnableAppInsights,
            CreatedUtc = DateTimeOffset.UtcNow
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("User created a new account with password.");

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
            await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

            await _signInManager.SignInAsync(user, isPersistent: false);
            await UpdateLastSuccessfulLoginAsync(user);
            _logger.LogInformation("User created a new account with password.");

            return RedirectToLocal(returnUrl);
        }
        AddErrors(result);

        // If we got this far, something failed, redisplay form
        return View(model);
    }

    private async Task<bool> IsTurnstileValidAsync()
    {
        var token = Request.Form["cf-turnstile-response"].ToString();
        return await _turnstileService.VerifyAsync(token, HttpContext.Connection.RemoteIpAddress, HttpContext.RequestAborted);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string returnUrl = null)
    {
        if (!_appSettingsService.IsExternalAuthenticationEnabled())
        {
            return NotFound();
        }

        // Request a redirect to the external login provider.
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(
        string returnUrl = null,
        string remoteError = null)
    {
        if (!_appSettingsService.IsExternalAuthenticationEnabled())
        {
            return NotFound();
        }

        if (remoteError != null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectToAction(nameof(Login));
        }
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded)
        {
            // Refresh locally stored profile fields with any fresher data from the provider.
            var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingUser != null)
            {
                await RefreshUserProfileFromExternalLoginAsync(existingUser, info);
                await UpdateLastSuccessfulLoginAsync(existingUser);
            }
            _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
            return RedirectToLocal(returnUrl);
        }
        if (result.IsLockedOut)
        {
            return RedirectToAction(nameof(Lockout));
        }

        // The user has no external-login record yet.
        // Auto-link to an existing local account if the email matches.
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrEmpty(email))
        {
            var localUser = await _userManager.FindByEmailAsync(email);
            if (localUser != null)
            {
                // Link the external provider to the existing account and sign in.
                var addResult = await _userManager.AddLoginAsync(localUser, info);
                if (addResult.Succeeded)
                {
                    await RefreshUserProfileFromExternalLoginAsync(localUser, info);
                    await _signInManager.SignInAsync(localUser, isPersistent: false);
                    await UpdateLastSuccessfulLoginAsync(localUser);
                    _logger.LogInformation(
                        "Linked {Provider} external login to existing account for {Email}.",
                        info.LoginProvider, email);
                    return RedirectToLocal(returnUrl);
                }
                // AddLoginAsync can fail if the login is already linked to a different account.
                foreach (var error in addResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }

        // No existing account — ask the user to confirm / complete their profile.
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["LoginProvider"] = info.LoginProvider;
        var (firstName, lastName) = GetNamesFromExternalClaims(info.Principal);
        return View("ExternalLogin", new ExternalLoginViewModel
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null)
    {
        if (!_appSettingsService.IsExternalAuthenticationEnabled())
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogWarning("External login confirmation attempted with missing provider information");
                ErrorMessage = "Error loading external login information. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            // Use placeholder names if the provider did not supply them;
            // the user will be prompted to complete their profile after sign-in.
            var firstName = string.IsNullOrWhiteSpace(model.FirstName) ? "User" : model.FirstName;
            var lastName  = string.IsNullOrWhiteSpace(model.LastName)
                ? (model.Email?.Split('@')[0] ?? "Unknown")
                : model.LastName;
            var needsProfileUpdate = string.IsNullOrWhiteSpace(model.FirstName)
                                  || string.IsNullOrWhiteSpace(model.LastName);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = firstName,
                LastName = lastName
            };
            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    await UpdateLastSuccessfulLoginAsync(user);
                    _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                    // Redirect to profile update when placeholder names were used.
                    if (needsProfileUpdate)
                    {
                        return RedirectToPage("/Account/Manage/UpdateProfile", new { area = "Identity",
                            StatusMessage = "Please complete your name to finish setting up your account." });
                    }
                    return RedirectToLocal(returnUrl);
                }
            }
            AddErrors(result);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(nameof(ExternalLogin), model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (userId == null || code == null)
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Email confirmation attempted with invalid user ID: '{UserId}'", userId);
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        var result = await _userManager.ConfirmEmailAsync(user, code);
        return View(result.Succeeded ? "ConfirmEmail" : "Error");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
            await _emailSender.SendEmailAsync(model.Email, "Reset SailScores Password",
                $"<br/>Please reset your SailScores password by clicking here:<br/> <a href='{callbackUrl}'>{callbackUrl}</a><br/>");
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

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
            _logger.LogWarning("Password reset attempted without reset code");
            return RedirectToAction(nameof(ForgotPassword));
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
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }
        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
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

    // from https://medium.com/@ozgurgul/asp-net-core-2-0-webapi-jwt-authentication-with-identity-mysql-3698eeba6ff8
    [HttpPost]
    [AllowAnonymous]
    public async Task<object> JwtToken([FromBody] LoginViewModel model)
    {
        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

        if (result.Succeeded)
        {
            var appUser = _userManager.Users.SingleOrDefault(r => r.Email == model.Email);
            await UpdateLastSuccessfulLoginAsync(appUser);
            return await GenerateJwtToken(model.Email, appUser);
        }

        throw new ApplicationException("INVALID_LOGIN_ATTEMPT");
    }

    #region Helpers

    private async Task UpdateLastSuccessfulLoginAsync(ApplicationUser user)
    {
        if (user == null)
        {
            return;
        }

        user.LastSuccessfulLoginUtc = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);
    }

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
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }


    private async Task<object> GenerateJwtToken(string email, ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_appSettingsService.GetJwtKey()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddDays(_appSettingsService.GetJwtExpireDays());
        var issuer = _appSettingsService.GetJwtIssuer();

        var token = new JwtSecurityToken(
            issuer,
            issuer,
            claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    // External login helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates FirstName / LastName on the local user record when the external
    /// provider supplies fresher values.  No-ops when claims are absent.
    /// </summary>
    private async Task RefreshUserProfileFromExternalLoginAsync(
        ApplicationUser user, ExternalLoginInfo info)
    {
        var (firstName, lastName) = GetNamesFromExternalClaims(info.Principal);
        var changed = false;

        if (!string.IsNullOrWhiteSpace(firstName) && user.FirstName != firstName)
        {
            user.FirstName = firstName;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(lastName) && user.LastName != lastName)
        {
            user.LastName = lastName;
            changed = true;
        }

        if (changed)
        {
            await _userManager.UpdateAsync(user);
        }
    }

    /// <summary>
    /// Extracts given name and surname from external-provider claims.
    /// Falls back to splitting ClaimTypes.Name when dedicated claims are absent
    /// (e.g. Apple on second and subsequent sign-ins).
    /// </summary>
    private static (string firstName, string lastName) GetNamesFromExternalClaims(
        ClaimsPrincipal principal)
    {
        var firstName = principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName  = principal.FindFirstValue(ClaimTypes.Surname);

        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            var fullName = principal.FindFirstValue(ClaimTypes.Name);
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                firstName = parts[0];
                lastName  = parts.Length > 1 ? parts[1] : string.Empty;
            }
        }

        return (firstName ?? string.Empty, lastName ?? string.Empty);
    }
}
