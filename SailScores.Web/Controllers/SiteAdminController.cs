using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class SiteAdminController : Controller
{
    private readonly IWebSiteAdminService _siteAdminService;
    private readonly IAuthorizationService _authService;

    public SiteAdminController(
        IWebSiteAdminService siteAdminService,
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
    public async Task<ActionResult> Backup(string clubInitials)
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        try
        {
            var (data, fileName) = await _siteAdminService.BackupClubAsync(
                clubInitials,
                User.Identity?.Name ?? "Unknown");

            return File(data, "application/gzip", fileName);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error creating backup: {ex.Message}";
            return RedirectToAction(nameof(Details), new { clubInitials });
        }
    }

    // POST: SiteAdmin/Reset
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Reset(Guid clubId, string clubInitials, ResetLevel resetLevel)
    {
        if (!await _authService.IsUserFullAdmin(User))
        {
            return Unauthorized();
        }

        try
        {
            await _siteAdminService.ResetClubAsync(clubId, resetLevel);
            
            var levelDescription = resetLevel switch
            {
                ResetLevel.RacesAndSeries => "Races and series have been removed. Competitors, fleets, boat classes, seasons, and scoring systems were preserved.",
                ResetLevel.RacesSeriesAndCompetitors => "Races, series, and competitors have been removed. Fleets, boat classes, seasons, and scoring systems were preserved.",
                ResetLevel.FullReset => "Full reset completed. All data has been removed and scoring systems reset to defaults.",
                _ => "Club has been reset successfully."
            };
            
            TempData["Message"] = levelDescription;
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error resetting club: {ex.Message}";
        }

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
