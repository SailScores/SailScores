using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Web.Authorization;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class ScoreCodeGroupController : Controller
{
    private readonly Core.Services.IClubService _clubService;
    private readonly Core.Services.IScoringService _scoringService;
    private readonly IAuthorizationService _authService;
    private readonly IRedirectHelper _redirectHelper;

    public ScoreCodeGroupController(
        Core.Services.IClubService clubService,
        Core.Services.IScoringService scoringService,
        IAuthorizationService authService,
        IRedirectHelper redirectHelper)
    {
        _clubService = clubService;
        _scoringService = scoringService;
        _authService = authService;
        _redirectHelper = redirectHelper;
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

        ValidateScoreCodeGroupConflicts(model, scoringSystem, null);

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

        return _redirectHelper.SafeRedirect(
            Url,
            Request,
            returnUrl,
            "Edit",
            "ScoringSystem");
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

        ValidateScoreCodeGroupConflicts(model, scoringSystem, model.Id);

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

        return _redirectHelper.SafeRedirect(
            Url,
            Request,
            returnUrl,
            "Edit",
            "ScoringSystem");
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

    /// <summary>
    /// Validates that the overage code and included codes don't conflict with other groups.
    /// Checks this system and all parent systems.
    /// </summary>
    private void ValidateScoreCodeGroupConflicts(
        ScoreCodeGroupViewModel model,
        ScoringSystem scoringSystem,
        Guid? currentGroupId)
    {
        var includedCodes = model.IncludedCodeNames ?? new List<string>();
        var overageCode = model.OverageCodeName;

        // Get all groups in this system and parent systems
        var allGroups = GetAllGroupsInHierarchy(scoringSystem);

        // Exclude the current group if editing
        var otherGroups = allGroups
            .Where(g => g.Id != currentGroupId)
            .ToList();

        // Check 1: Overage code should not be in this group's included codes
        if (includedCodes.Contains(overageCode))
        {
            ModelState.AddModelError(
                nameof(model.OverageCodeName),
                "The overage code cannot be one of the included codes in this group.");
        }

        // Check 2: Overage code should not be used as overage code in any other group
        var conflictingOverageGroups = otherGroups
            .Where(g => g.OverageCodeName == overageCode)
            .ToList();

        if (conflictingOverageGroups.Any())
        {
            var groupNames = string.Join(", ", conflictingOverageGroups.Select(g => g.DisplayName));
            ModelState.AddModelError(
                nameof(model.OverageCodeName),
                $"The code '{overageCode}' is already used as the overage code in these groups: {groupNames}");
        }

        // Check 3: Included codes should not be used as overage code in any other group
        var conflictingIncludedCodes = includedCodes
            .Where(code => otherGroups.Any(g => g.OverageCodeName == code))
            .ToList();

        if (conflictingIncludedCodes.Any())
        {
            var codeList = string.Join(", ", conflictingIncludedCodes);
            ModelState.AddModelError(
                nameof(model.IncludedCodeNames),
                $"These codes are already used as overage codes in other groups: {codeList}");
        }
    }

    /// <summary>
    /// Gets all score code groups in this system and all parent systems.
    /// </summary>
    private static List<ScoreCodeGroup> GetAllGroupsInHierarchy(ScoringSystem system)
    {
        var allGroups = new List<ScoreCodeGroup>();

        if (system?.ScoreCodeGroups != null)
        {
            allGroups.AddRange(system.ScoreCodeGroups);
        }

        if (system?.InheritedScoreCodeGroups != null)
        {
            allGroups.AddRange(system.InheritedScoreCodeGroups);
        }

        return allGroups;
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

        return _redirectHelper.SafeRedirect(
            Url,
            Request,
            returnUrl,
            "Edit",
            "ScoringSystem");
    }
}
