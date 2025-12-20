using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SailScores.Identity.Entities;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;


namespace SailScores.Web.Controllers;

public class SupporterController : Controller
{
    private readonly ISupporterService _supporterService;
    private readonly IAuthorizationService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IStripeService _stripeService;
    private readonly AppSettingsService _appSettingsService;

    public SupporterController(
        ISupporterService supporterService,
        IAuthorizationService authService,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IStripeService stripeService,
        AppSettingsService appSettingsService)
    {
        _supporterService = supporterService;
        _authService = authService;
        _userManager = userManager;
        _configuration = configuration;
        _stripeService = stripeService;
        _appSettingsService = appSettingsService;
    }

    public async Task<ActionResult> Index()
    {
        var isAdmin = await _authService.IsUserFullAdmin(User);
        var supporters = isAdmin
            ? await _supporterService.GetAllSupportersAsync()
            : await _supporterService.GetVisibleSupportersAsync();

        ViewBag.IsAdmin = isAdmin;
        ViewData["StripePublishableKey"] = _configuration["Stripe:PublishableKey"];
        return View(supporters);
    }

    public async Task<ActionResult> Success(string session_id)
    {
        if (string.IsNullOrWhiteSpace(session_id))
        {
            return RedirectToAction("Index");
        }

        // Retrieve session info if user is authenticated (optional - we can show generic success without it)
        string clubInitials = null;
        bool multipleClubs = false;
        if (User?.Identity?.IsAuthenticated == true)
        {
            var email = User.Identity.Name;
            multipleClubs = await UserHasMultipleClubsAsync(email);
            var clubInfo = await _stripeService.GetFirstClubForUserEmailAsync(email);
            clubInitials = clubInfo.ClubInitials;
        }

        ViewBag.ClubInitials = clubInitials;
        ViewBag.SessionId = session_id;
        ViewBag.MultipleClubs = multipleClubs;

        return View();
    }

    private async Task<bool> UserHasMultipleClubsAsync(string email) => throw new NotImplementedException();

    public ActionResult Cancel()
    {
        return View();
    }

    public ActionResult Error()
    {
        return View();
    }

    [Authorize]
    public async Task<ActionResult> Create()
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        var vm = await _supporterService.GetBlankSupporter();
        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(SupporterWithOptionsViewModel model)
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.CreatedBy = await GetUserStringAsync();
        model.CreatedDate = DateTime.UtcNow;
        await _supporterService.SaveNew(model);

        return RedirectToAction("Index");
    }

    [Authorize]
    public async Task<ActionResult> Edit(Guid id)
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        var supporter = await _supporterService.GetSupporterAsync(id);
        if (supporter == null)
        {
            return NotFound();
        }

        return View(supporter);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(SupporterWithOptionsViewModel model)
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.UpdatedBy = await GetUserStringAsync();
        model.UpdatedDate = DateTime.UtcNow;
        await _supporterService.Update(model);

        return RedirectToAction("Index");
    }

    [Authorize]
    public async Task<ActionResult> Delete(Guid id)
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        var supporter = await _supporterService.GetSupporterAsync(id);
        if (supporter == null)
        {
            return NotFound();
        }

        return View(supporter);
    }

    [HttpPost]
    [ActionName("Delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> PostDelete(Guid id)
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        await _supporterService.Delete(id);
        return RedirectToAction("Index");
    }

    public async Task<FileStreamResult> GetLogo(Guid id)
    {
        // Move logic to service
        return await _supporterService.GetLogoAsync(id);
    }

    private async Task<string> GetUserStringAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.GetDisplayName() ?? User.Identity?.Name ?? "Unknown";
    }
}
