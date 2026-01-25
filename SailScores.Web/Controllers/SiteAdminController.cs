using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class SiteAdminController : Controller
{
    private readonly ISiteAdminService _siteAdminService;
    private readonly IAuthorizationService _authService;

    public SiteAdminController(
        ISiteAdminService siteAdminService,
        IAuthorizationService authService)
    {
        _siteAdminService = siteAdminService;
        _authService = authService;
    }

    // GET: SiteAdmin
    public async Task<ActionResult> Index()
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        var vm = await _siteAdminService.GetAllClubsAsync();
        return View(vm);
    }

    // POST: SiteAdmin/ResetCache
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ResetCache()
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        await _siteAdminService.ResetClubInitialsCacheAsync();
        TempData["Message"] = "Club initials cache has been reset successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: SiteAdmin/Details/abc
    public async Task<ActionResult> Details(string clubInitials)
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        var vm = await _siteAdminService.GetClubDetailsAsync(clubInitials);
        if (vm == null)
        {
            return NotFound();
        }

        return View(vm);
    }

    // POST: SiteAdmin/Backup
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Backup(Guid clubId)
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        var backupData = await _siteAdminService.BackupClubAsync(clubId);
        if (backupData == null)
        {
            return NotFound();
        }

        var fileName = $"club-backup-{clubId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        return File(System.Text.Encoding.UTF8.GetBytes(backupData), "application/json", fileName);
    }

    // POST: SiteAdmin/Reset
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Reset(Guid clubId, string clubInitials)
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        await _siteAdminService.ResetClubAsync(clubId);
        TempData["Message"] = "Club has been reset successfully. All races, scores, and competitors have been removed.";
        return RedirectToAction(nameof(Details), new { clubInitials });
    }

    // POST: SiteAdmin/RecalculateSeries
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> RecalculateSeries(Guid seriesId, string clubInitials)
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        var email = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
        await _siteAdminService.RecalculateSeriesAsync(seriesId, email);
        TempData["Message"] = "Series has been recalculated successfully.";
        return RedirectToAction(nameof(Details), new { clubInitials });
    }
}
