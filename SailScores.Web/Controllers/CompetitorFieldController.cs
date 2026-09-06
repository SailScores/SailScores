using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Core.Services.Interfaces;
using SailScores.Web.Authorization;

namespace SailScores.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
public class CompetitorFieldController : Controller
{
    private readonly IClubService _clubService;
    private readonly ICompetitorFieldService _competitorFieldService;

    public CompetitorFieldController(IClubService clubService, ICompetitorFieldService competitorFieldService)
    {
        _clubService = clubService;
        _competitorFieldService = competitorFieldService;
    }

    public async Task<IActionResult> Index(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;
        var clubId = await _clubService.GetClubId(clubInitials);
        // Admin index should show all definitions (including inactive) so admins can reactivate if needed
        var fields = await _competitorFieldService.GetAllFieldDefinitionsAsync(clubId);
        return View(fields);
    }

    public async Task<IActionResult> Create(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;
        var clubId = await _clubService.GetClubId(clubInitials);
        return View(new CompetitorFieldDefinition
        {
            ClubId = clubId,
            DataType = CustomFieldDataType.Text,
            DisplayOrder = 0,
            IsActive = true,
            HighlyVisible = false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string clubInitials, CompetitorFieldDefinition model)
    {
        ViewData["ClubInitials"] = clubInitials;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var clubId = await _clubService.GetClubId(clubInitials);
        model.ClubId = clubId;
        await _competitorFieldService.SaveFieldDefinitionAsync(model);
        return RedirectToAction(nameof(Index), new { clubInitials });
    }

    public async Task<IActionResult> Edit(string clubInitials, Guid id)
    {
        ViewData["ClubInitials"] = clubInitials;
        var field = await _competitorFieldService.GetFieldDefinitionAsync(id);
        if (field == null)
        {
            return NotFound();
        }

        return View(field);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string clubInitials, Guid id, CompetitorFieldDefinition model)
    {
        ViewData["ClubInitials"] = clubInitials;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Id = id;
        var clubId = await _clubService.GetClubId(clubInitials);
        model.ClubId = clubId;
        await _competitorFieldService.SaveFieldDefinitionAsync(model);
        return RedirectToAction(nameof(Index), new { clubInitials });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string clubInitials, Guid id)
    {
        // Keep Delete action for backward compatibility: mark inactive
        await _competitorFieldService.SetFieldActiveStateAsync(id, false);
        return RedirectToAction(nameof(Index), new { clubInitials });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inactivate(string clubInitials, Guid id)
    {
        await _competitorFieldService.SetFieldActiveStateAsync(id, false);
        return RedirectToAction(nameof(Index), new { clubInitials });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePermanent(string clubInitials, Guid id)
    {
        await _competitorFieldService.DeleteFieldDefinitionPermanentlyAsync(id);
        return RedirectToAction(nameof(Index), new { clubInitials });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(string clubInitials, Guid id)
    {
        await _competitorFieldService.SetFieldActiveStateAsync(id, true);
        return RedirectToAction(nameof(Index), new { clubInitials });
    }
}
