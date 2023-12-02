using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using SailScores.Identity.Entities;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class MergeCompetitorController : Controller
{
    private readonly Core.Services.IClubService _clubService;
    private readonly ICompetitorService _competitorService;
    private readonly IAuthorizationService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMergeService _mergeService;

    public MergeCompetitorController(
        Core.Services.IClubService clubService,
        ICompetitorService competitorService,
        IAuthorizationService authService,
        UserManager<ApplicationUser> userManager,
        IMergeService mergeService)
    {
        _clubService = clubService;
        _competitorService = competitorService;
        _authService = authService;
        _userManager = userManager;
        _mergeService = mergeService;
    }

    public async Task<ActionResult> Options(string clubInitials)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }
        IList<Core.Model.Competitor> competitors = await _competitorService.GetCompetitorsAsync(clubId, false);
        var vm = new MergeCompetitorViewModel
        {
            TargetCompetitorOptions = competitors.OrderBy(c => c.Name).ToList()
        };
        return View("SelectTarget", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Options(
        string clubInitials,
        MergeCompetitorViewModel vm)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }

        vm.SourceCompetitorOptions = await _mergeService.GetSourceOptionsFor(vm.TargetCompetitorId);
        return View("SelectSource", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Verify(
        string clubInitials,
        MergeCompetitorViewModel vm)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }

        vm.SourceCompetitor = await _competitorService.GetCompetitorAsync(vm.SourceCompetitorId.Value);
        vm.TargetCompetitor = await _competitorService.GetCompetitorAsync(vm.TargetCompetitorId.Value);
        if (vm.SourceCompetitor.ClubId != clubId ||
            vm.TargetCompetitor.ClubId != clubId)
        {
            return Unauthorized();
        }
        vm.SourceNumberOfRaces = await _mergeService.GetNumberOfRaces(vm.SourceCompetitorId.Value);
        vm.TargetNumberOfRaces = await _mergeService.GetNumberOfRaces(vm.TargetCompetitorId.Value);

        vm.SourceSeasons = await _mergeService.GetSeasons(vm.SourceCompetitorId.Value);
        vm.TargetSeasons = await _mergeService.GetSeasons(vm.TargetCompetitorId.Value);

        return View("Verify", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Merge(
        string clubInitials,
        MergeCompetitorViewModel vm)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }
        vm.SourceCompetitor = await _competitorService.GetCompetitorAsync(vm.SourceCompetitorId.Value);
        vm.TargetCompetitor = await _competitorService.GetCompetitorAsync(vm.TargetCompetitorId.Value);
        if (vm.SourceCompetitor.ClubId != clubId ||
            vm.TargetCompetitor.ClubId != clubId)
        {
            return Unauthorized();
        }
        await _mergeService.Merge(
            vm.TargetCompetitorId,
            vm.SourceCompetitorId,
            await GetUserStringAsync());
        vm.TargetNumberOfRaces = await _mergeService.GetNumberOfRaces(vm.TargetCompetitorId);

        return View("Done", vm);
    }

    private async Task<string> GetUserStringAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user.GetDisplayName();
    }
}