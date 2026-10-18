using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using SailScores.Web.Authorization;
using SailScores.Web.Models.SailScores;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class ScoringSystemController : Controller
{

    private readonly IClubService _clubService;
    private readonly IScoringService _scoringService;
    private readonly IMapper _mapper;
    private readonly IAuthorizationService _authService;

    public ScoringSystemController(
        IClubService clubService,
        IScoringService scoringService,
        IAuthorizationService authService,
        IMapper mapper)
    {
        _clubService = clubService;
        _scoringService = scoringService;
        _authService = authService;
        _mapper = mapper;
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Create(
        string clubInitials)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        var club = await _clubService.GetMinimalClub(clubId);

        var vm = new ScoringSystemWithOptionsViewModel
        {
            ClubId = clubId,
            ParentSystemId = club.DefaultScoringSystemId
        };
        var potentialParents = await _scoringService.GetScoringSystemsAsync(clubId, true);
        vm.ParentSystemOptions = potentialParents.ToList();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Create(
        string clubInitials,
        ScoringSystemWithOptionsViewModel model)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        model.ClubId = clubId;
        model.Id = Guid.NewGuid();
        var potentialParents = await _scoringService.GetScoringSystemsAsync(clubId, true);
        var parent = potentialParents.FirstOrDefault(s => s.Id == model.ParentSystemId);
        if (model.ParentSystemId.HasValue && parent == null)
        {
            ModelState.AddModelError(nameof(model.ParentSystemId),
                "That parent scoring system isn't available to this club.");
        }
        AddSettingsValidationErrors(model, parent);
        if (!ModelState.IsValid)
        {
            model.ParentSystemOptions = potentialParents.ToList();
            return View(model);
        }

        await _scoringService.SaveScoringSystemAsync(
            ToScoringSystemForSave(model, parent));

        return RedirectToAction("Edit", "ScoringSystem", new { id = model.Id });
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Edit(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        var scoringSystem = await _scoringService.GetScoringSystemAsync(id);
        if (scoringSystem.ClubId != clubId)
        {
            return Unauthorized();
        }

        var vm = _mapper.Map<ScoringSystemWithOptionsViewModel>(scoringSystem);
        vm.ParentSystemOptions = await GetParentOptionsAsync(clubId, scoringSystem);

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Edit(
        string clubInitials,
        ScoringSystemWithOptionsViewModel model)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (model.ClubId != clubId)
        {
            return Unauthorized();
        }
        var scoringSystem = await _scoringService.GetScoringSystemAsync(model.Id);
        var potentialParents = await GetParentOptionsAsync(clubId, scoringSystem);
        var parent = potentialParents.FirstOrDefault(s => s.Id == model.ParentSystemId);
        if (model.ParentSystemId.HasValue && parent == null)
        {
            ModelState.AddModelError(nameof(model.ParentSystemId),
                "That parent scoring system isn't available to this club.");
        }
        AddSettingsValidationErrors(model, parent);
        if (!ModelState.IsValid)
        {
            model.ParentSystemOptions = potentialParents;

            model.ScoreCodes = scoringSystem.ScoreCodes;
            model.InheritedScoreCodes = scoringSystem.InheritedScoreCodes;
            model.ScoreCodeGroups = scoringSystem.ScoreCodeGroups;
            model.InheritedScoreCodeGroups = scoringSystem.InheritedScoreCodeGroups;
            return View(model);
        }
        await _scoringService.SaveScoringSystemAsync(
            ToScoringSystemForSave(model, parent));

        return RedirectToAction("Index", "Admin");

    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Delete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        var scoringSystem = await _scoringService.GetScoringSystemAsync(id);
        if (scoringSystem.ClubId != clubId)
        {
            return Unauthorized();
        }

        var vm = _mapper.Map<ScoringSystemDeleteViewModel>(scoringSystem);
        vm.IsDeletable = !(await _scoringService.IsScoringSystemInUseAsync(vm.Id));

        return View(vm);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        var scoringSystem = await _scoringService.GetScoringSystemAsync(id);
        if (scoringSystem.ClubId != clubId)
        {
            return Unauthorized();
        }

        if (await _scoringService.IsScoringSystemInUseAsync(id))
        {
            return StatusCode(500);
        }

        await _scoringService.DeleteScoringSystemAsync(id);
        return RedirectToAction("Index", "Admin");
    }

    // Discard pattern and calculator settings are validated against what the
    // system inherits from its (resolved) parent.
    private void AddSettingsValidationErrors(
        ScoringSystemWithOptionsViewModel model,
        ScoringSystem parent)
    {
        foreach (var (propertyName, message) in
            ScoringCalculatorRegistry.GetValidationErrors(model, parent))
        {
            var key = propertyName == nameof(ScoringSystem.DiscardPattern)
                || propertyName == nameof(ScoringSystem.DiscardMethod)
                ? propertyName
                : $"{nameof(ScoringSystem.Settings)}.{propertyName}";
            ModelState.AddModelError(key, message);
        }
    }

    // The club's systems and the shared systems it can use. A system keeps its current
    // parent as an option even if that shared system was later restricted to other clubs.
    private async Task<IList<ScoringSystem>> GetParentOptionsAsync(
        Guid clubId,
        ScoringSystem system)
    {
        var options = (await _scoringService.GetScoringSystemsAsync(clubId, true))
            .Where(s => s.Id != system?.Id)
            .ToList();
        var currentParentId = system?.ParentSystemId;
        if (currentParentId.HasValue && options.All(s => s.Id != currentParentId.Value))
        {
            var currentParent = await _scoringService.GetScoringSystemAsync(currentParentId.Value);
            if (currentParent != null)
            {
                options.Add(currentParent);
                options = options.OrderBy(s => s.Name).ToList();
            }
        }
        return options;
    }

    private ScoringSystem ToScoringSystemForSave(
        ScoringSystemWithOptionsViewModel model,
        ScoringSystem parent)
    {
        var system = _mapper.Map<ScoringSystem>(model);
        system.Settings = ScoringCalculatorRegistry.RemoveUnusedSettings(
            model.Settings,
            parent?.CalculatorType ?? model.CalculatorType);
        return system;
    }
}
