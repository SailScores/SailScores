using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Authorization;
using SailScores.Web.Models.SailScores;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class ScoreCodeGroupController : Controller
{
    private readonly IClubService _clubService;
    private readonly IScoringService _scoringService;
    private readonly IAuthorizationService _authService;

    public ScoreCodeGroupController(
        IClubService clubService,
        IScoringService scoringService,
        IAuthorizationService authService)
    {
        _clubService = clubService;
        _scoringService = scoringService;
        _authService = authService;
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Create(
        string clubInitials,
        Guid scoringSystemId,
        string returnUrl = null)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var scoringSystem = await _scoringService.GetScoringSystemAsync(scoringSystemId);

        if (scoringSystem.ClubId != clubId)
        {
            return Unauthorized();
        }

        // Get all available codes for this system (own + inherited)
        var allScoreCodes = scoringSystem.ScoreCodes.Concat(scoringSystem.InheritedScoreCodes).ToList();

        var vm = new ScoreCodeGroupViewModel(
            new ScoreCodeGroup { ScoringSystemId = scoringSystemId },
            allScoreCodes);

        vm.IncludedCodeNames = new List<string>();
        ViewBag.ReturnUrl = returnUrl ?? $"/{clubInitials}/ScoringSystem/Edit/{scoringSystemId}";


        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Create(
        string clubInitials,
        ScoreCodeGroupViewModel model,
        string returnUrl = null)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var scoringSystem = await _scoringService.GetScoringSystemAsync(model.ScoringSystemId);

        if (scoringSystem.ClubId != clubId)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            var allScoreCodes = scoringSystem.ScoreCodes.Concat(scoringSystem.InheritedScoreCodes).ToList();
            model.AvailableCodes = allScoreCodes;
            ViewBag.ReturnUrl = returnUrl ?? $"/{clubInitials}/ScoringSystem/Edit/{model.ScoringSystemId}";
            return View(model);
        }

        // Map from ViewModel to Core Model to DB Entity and save
        model.Id = Guid.NewGuid();
        var coreGroup = new ScoreCodeGroup(
            model.Id,
            model.ScoringSystemId,
            model.Name,
            model.LimitationType,
            model.LimitationValue,
            model.UseNonDiscardedRaces,
            model.OverageCodeName,
            model.OverageSelectionMethod,
            model.IncludedCodeNames ?? new List<string>());

        await _scoringService.SaveScoreCodeGroupAsync(coreGroup);

        return Redirect(returnUrl ?? $"/{clubInitials}/ScoringSystem/Edit/{model.ScoringSystemId}");
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Edit(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var group = await _scoringService.GetScoreCodeGroupAsync(id);

        if (group == null)
        {
            return NotFound();
        }

        var scoringSystem = await _scoringService.GetScoringSystemAsync(group.ScoringSystemId);
        if (scoringSystem.ClubId != clubId)
        {
            return Unauthorized();
        }

        var allScoreCodes = scoringSystem.ScoreCodes.Concat(scoringSystem.InheritedScoreCodes).ToList();
        var vm = new ScoreCodeGroupViewModel(group, allScoreCodes);
        ViewBag.ReturnUrl = returnUrl ?? $"/{clubInitials}/ScoringSystem/Edit/{group.ScoringSystemId}";

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Edit(
        string clubInitials,
        ScoreCodeGroupViewModel model,
        string returnUrl = null)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var scoringSystem = await _scoringService.GetScoringSystemAsync(model.ScoringSystemId);

        if (scoringSystem.ClubId != clubId)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            var allScoreCodes = scoringSystem.ScoreCodes.Concat(scoringSystem.InheritedScoreCodes).ToList();
            model.AvailableCodes = allScoreCodes;
            ViewBag.ReturnUrl = returnUrl ?? $"/{clubInitials}/ScoringSystem/Edit/{model.ScoringSystemId}";
            return View(model);
        }

        var coreGroup = new ScoreCodeGroup(
            model.Id,
            model.ScoringSystemId,
            model.Name,
            model.LimitationType,
            model.LimitationValue,
            model.UseNonDiscardedRaces,
            model.OverageCodeName,
            model.OverageSelectionMethod,
            model.IncludedCodeNames ?? new List<string>());

        await _scoringService.SaveScoreCodeGroupAsync(coreGroup);

        return Redirect(returnUrl ?? $"/{clubInitials}/ScoringSystem/Edit/{model.ScoringSystemId}");
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Delete(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var group = await _scoringService.GetScoreCodeGroupAsync(id);

        if (group == null)
        {
            return NotFound();
        }

        var scoringSystem = await _scoringService.GetScoringSystemAsync(group.ScoringSystemId);
        if (scoringSystem.ClubId != clubId)
        {
            return Unauthorized();
        }

        ViewBag.ReturnUrl = returnUrl ?? $"/{clubInitials}/ScoringSystem/Edit/{group.ScoringSystemId}";
        return View(group);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> DeleteConfirmed(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var group = await _scoringService.GetScoreCodeGroupAsync(id);

        if (group == null)
        {
            return NotFound();
        }

        var scoringSystem = await _scoringService.GetScoringSystemAsync(group.ScoringSystemId);
        if (scoringSystem.ClubId != clubId)
        {
            return Unauthorized();
        }

        var scoringSystemId = group.ScoringSystemId;
        await _scoringService.DeleteScoreCodeGroupAsync(id);

        return Redirect(returnUrl ?? $"/{clubInitials}/ScoringSystem/Edit/{scoringSystemId}");
    }
}
