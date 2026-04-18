using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Authorization;
using SailScores.Web.Models.SailScores;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class HandicapSystemController : Controller
{
    private readonly IClubService _clubService;
    private readonly IHandicapService _handicapService;
    private readonly IAuthorizationService _authService;

    public HandicapSystemController(
        IClubService clubService,
        IHandicapService handicapService,
        IAuthorizationService authService)
    {
        _clubService = clubService;
        _handicapService = handicapService;
        _authService = authService;
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Create(string clubInitials)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var vm = new HandicapSystemViewModel
        {
            ClubId = clubId,
            SystemType = HandicapSystemType.PhrfToD
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Create(string clubInitials, HandicapSystemViewModel model)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        model.ClubId = clubId;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _handicapService.SaveHandicapSystemAsync(new HandicapSystem
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = model.Name,
            SystemType = model.SystemType,
            Description = model.Description
        });

        return RedirectToAction("Index", "Admin");
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Edit(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var system = await _handicapService.GetHandicapSystemAsync(id);

        if (system == null || system.ClubId != clubId)
            return Unauthorized();

        return View(new HandicapSystemViewModel
        {
            Id = system.Id,
            ClubId = system.ClubId,
            Name = system.Name,
            SystemType = system.SystemType,
            Description = system.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Edit(string clubInitials, HandicapSystemViewModel model)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        if (model.ClubId != clubId)
            return Unauthorized();

        if (!ModelState.IsValid)
            return View(model);

        await _handicapService.SaveHandicapSystemAsync(new HandicapSystem
        {
            Id = model.Id,
            ClubId = clubId,
            Name = model.Name,
            SystemType = model.SystemType,
            Description = model.Description
        });

        return RedirectToAction("Index", "Admin");
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Delete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var system = await _handicapService.GetHandicapSystemAsync(id);

        if (system == null || system.ClubId != clubId)
            return Unauthorized();

        return View(new HandicapSystemDeleteViewModel
        {
            Id = system.Id,
            ClubId = system.ClubId,
            Name = system.Name,
            SystemType = system.SystemType,
            Description = system.Description,
            IsDeletable = true
        });
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var system = await _handicapService.GetHandicapSystemAsync(id);

        if (system == null || system.ClubId != clubId)
            return Unauthorized();

        await _handicapService.DeleteHandicapSystemAsync(id);
        return RedirectToAction("Index", "Admin");
    }
}
