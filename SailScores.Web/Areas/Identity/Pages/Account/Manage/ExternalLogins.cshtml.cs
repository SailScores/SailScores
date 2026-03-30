using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SailScores.Identity.Entities;
using SailScores.Web.Services;

namespace SailScores.Web.Areas.Identity.Pages.Account.Manage
{
    public class ExternalLoginsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly AppSettingsService _appSettingsService;

        public ExternalLoginsModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserStore<ApplicationUser> userStore,
            AppSettingsService appSettingsService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userStore = userStore;
            _appSettingsService = appSettingsService;
        }

        /// <summary>External login providers currently linked to this account.</summary>
        public IList<UserLoginInfo> CurrentLogins { get; set; }

        /// <summary>Configured providers that are NOT yet linked to this account.</summary>
        public IList<AuthenticationScheme> OtherLogins { get; set; }

        /// <summary>
        /// Whether the Remove button should be shown.
        /// Only shown when the user has a password set, or has more than one
        /// external login, so they cannot accidentally lock themselves out.
        /// </summary>
        public bool ShowRemoveButton { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            CurrentLogins = await _userManager.GetLoginsAsync(user);

            OtherLogins = _appSettingsService.IsExternalAuthenticationEnabled()
                ? (await _signInManager.GetExternalAuthenticationSchemesAsync())
                    .Where(scheme => CurrentLogins.All(ul => ul.LoginProvider != scheme.Name))
                    .ToList()
                : new List<AuthenticationScheme>();

            // The user can only remove a login if another sign-in method exists.
            string passwordHash = null;
            if (_userStore is IUserPasswordStore<ApplicationUser> passwordStore)
            {
                passwordHash = await passwordStore.GetPasswordHashAsync(user, HttpContext.RequestAborted);
            }
            ShowRemoveButton = passwordHash != null || CurrentLogins.Count > 1;

            return Page();
        }

        public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                StatusMessage = "Error: The external login was not removed.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "The external login was removed.";
            return RedirectToPage();
        }

        /// <summary>
        /// Initiates the OAuth flow to link an additional external provider to the
        /// currently signed-in account.
        /// </summary>
        public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
        {
            if (!_appSettingsService.IsExternalAuthenticationEnabled())
            {
                StatusMessage = "Error: External login providers are currently unavailable.";
                return RedirectToPage();
            }

            // Clear any leftover external cookie before starting a new challenge.
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var redirectUrl = Url.Page("./ExternalLogins", pageHandler: "LinkLoginCallback");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        /// <summary>
        /// Handles the OAuth callback after the user has authenticated with the
        /// external provider.  Adds the login to the existing account.
        /// </summary>
        public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
        {
            if (!_appSettingsService.IsExternalAuthenticationEnabled())
            {
                StatusMessage = "Error: External login providers are currently unavailable.";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var info = await _signInManager.GetExternalLoginInfoAsync(userId);
            if (info == null)
            {
                StatusMessage = "Error: Could not load external login information. Please try again.";
                return RedirectToPage();
            }

            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                StatusMessage = "Error: The external login was not added. " +
                    "An external login can only be linked to one account at a time.";
                return RedirectToPage();
            }

            // Clear the transient external cookie.
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            StatusMessage = $"{info.ProviderDisplayName} was successfully added to your account.";
            return RedirectToPage();
        }
    }
}
