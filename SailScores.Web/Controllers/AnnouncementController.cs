using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SailScores.Identity.Entities;
using SailScores.Web.Authorization;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

public class AnnouncementController : Controller
{


    private readonly CoreServices.IClubService _clubService;
    private readonly IAnnouncementService _announcementService;
    private readonly IAuthorizationService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly IHtmlSanitizer _sanitizer;

    public AnnouncementController(
        CoreServices.IClubService clubService,
        IAnnouncementService announcementService,
        IAuthorizationService authService,
        UserManager<ApplicationUser> userManager,
        IHtmlSanitizer sanitizer,
        IMapper mapper)
    {
        _clubService = clubService;
        _announcementService = announcementService;
        _authService = authService;
        _userManager = userManager;
        _sanitizer = sanitizer;
        _mapper = mapper;
    }

    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> Create(
        string clubInitials,
        Guid regattaId,
        string returnUrl = null)
    {

        ViewData["ReturnUrl"] = returnUrl;
        var vm = await _announcementService.GetBlankAnnouncementForRegatta(
            clubInitials,
            regattaId);

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> Create(
        string clubInitials,
        AnnouncementWithOptions model,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);

            model.ClubId = clubId;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Content = _sanitizer.Sanitize(model.Content);
            model.CreatedBy = await GetUserStringAsync();
            model.CreatedDate = DateTime.UtcNow;
            model.CreatedLocalDate = DateTime.UtcNow.AddMinutes(0 - model.TimeOffset);
            await _announcementService.SaveNew(model);
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            AnnouncementWithOptions vm;
            if (model.RegattaId.HasValue)
            {
                vm = await _announcementService.GetBlankAnnouncementForRegatta(
                    clubInitials,
                    model.RegattaId.Value);
                
            } else
            {
                vm = model;
            }
            return View(vm);
        }
    }

    [Authorize]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> Edit(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        try
        {
            ViewData["ReturnUrl"] = returnUrl;

            var announcement =
                await _announcementService.GetAnnouncement(id);
            return View(announcement);
        }
        catch
        {
            return RedirectToAction("Index", "Admin");
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> Edit(
        string clubInitials,
        AnnouncementWithOptions model,
        string returnUrl = null)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Content = _sanitizer.Sanitize(model.Content);
            model.UpdatedBy = await GetUserStringAsync();
            model.UpdatedDate = DateTime.UtcNow;
            model.UpdatedLocalDate = DateTime.UtcNow.AddMinutes(0 - model.TimeOffset);
            await _announcementService.Update(model);

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            return View(model);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> Delete(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        var announcement = await _announcementService.GetAnnouncement(id);
        return View(announcement);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> PostDelete(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        try
        {
            await _announcementService.Delete(id);

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            var fleet = await _announcementService.GetAnnouncement(id);
            return View(fleet);
        }
    }

    private async Task<string> GetUserStringAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user.GetDisplayName();
    }
}
