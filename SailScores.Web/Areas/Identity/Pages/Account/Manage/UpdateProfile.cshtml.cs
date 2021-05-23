using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using SailScores.Identity.Entities;

namespace SailScores.Web.Areas.Identity.Pages.Account.Manage
{
    public class UpdateProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<UpdateProfileModel> _logger;

        public UpdateProfileModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<UpdateProfileModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public List<SelectListItem> SpeechRecognitionLanguageOptions { get; set; } = new
            List<SelectListItem>
                {
                    new SelectListItem( "English - AU", "en-AU"),
                    new SelectListItem( "English - US", "en-US"),
                    new SelectListItem( "Finnish - FI", "fi-FI"),
                    new SelectListItem( "Swedish - SE", "sv-SE")
                };

        public class InputModel
        {

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Display(Name = "Enable Browser Analytics")]
            public bool EnableAppInsights { get; set; }

            [Display(Name = "Speech Recognition Language")]
            public string SpeechRecognitionLanguage { get; set; }

        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            if(Input == null)
            {
                Input = new InputModel();
            }
            Input.FirstName = user.FirstName;
            Input.LastName = user.LastName;
            Input.EnableAppInsights = user.EnableAppInsights ?? false;
            Input.SpeechRecognitionLanguage = user.SpeechRecognitionLanguage ?? "en-US";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.EnableAppInsights = Input.EnableAppInsights;
            user.SpeechRecognitionLanguage = Input.SpeechRecognitionLanguage;

            var updateUserResult = await _userManager.UpdateAsync(user);
            if (!updateUserResult.Succeeded)
            {
                foreach (var error in updateUserResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User updated their profile successfully.");
            StatusMessage = "Your profile has been updated.";

            return RedirectToPage();
        }
    }
}
