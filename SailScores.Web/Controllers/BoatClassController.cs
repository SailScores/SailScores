using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Web.Authorization;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class BoatClassController : Controller
{

    private readonly CoreServices.IClubService _clubService;
    private readonly IBoatClassService _classService;
    private readonly CoreServices.IHandicapService _handicapService;
    private readonly IAuthorizationService _authService;

    public BoatClassController(
        CoreServices.IClubService clubService,
        IBoatClassService classService,
        CoreServices.IHandicapService handicapService,
        IAuthorizationService authService)
    {
        _clubService = clubService;
        _classService = classService;
        _handicapService = handicapService;
        _authService = authService;
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public ActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Create(
        string clubInitials,
        BoatClass model)
    {
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            model.ClubId = clubId;
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _classService.SaveNew(model);

            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            ModelState.AddModelError(String.Empty,
                "An error occurred saving these changes.");
            return View(model);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Edit(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var boatClass = await _classService.GetClass(id);
        var vm = new BoatClassWithHandicapsViewModel
        {
            Id = boatClass.Id,
            ClubId = boatClass.ClubId,
            Name = boatClass.Name,
            Description = boatClass.Description
        };
        var club = await _clubService.GetMinimalClub(clubId);
        if (club.EnableHandicapScoring)
            vm.HandicapRatings = await _handicapService.GetClassHandicapsAsync(id);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Edit(
        string clubInitials,
        BoatClassWithHandicapsViewModel model)
    {
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            var boatClass = await _classService.GetClass(model.Id);
            if (boatClass.ClubId != clubId)
                return Unauthorized();

            if (!ModelState.IsValid)
                return View(model);

            await _classService.Update(new BoatClass
            {
                Id = model.Id,
                ClubId = model.ClubId,
                Name = model.Name,
                Description = model.Description
            });

            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            return View(model);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> AddHandicap(string clubInitials, Guid boatClassId)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var boatClass = await _classService.GetClass(boatClassId);
        var systems = await _handicapService.GetHandicapSystemsAsync(clubId);
        return View(new ClassHandicapViewModel
        {
            BoatClassId = boatClassId,
            ClassName = boatClass.Name,
            ClubInitials = clubInitials,
            HandicapSystemOptions = systems
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> AddHandicap(string clubInitials, ClassHandicapViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            model.HandicapSystemOptions = await _handicapService.GetHandicapSystemsAsync(clubId);
            return View(model);
        }
        try
        {
            await _handicapService.SaveClassHandicapAsync(new ClassHandicap
            {
                Id = Guid.Empty,
                BoatClassId = model.BoatClassId,
                HandicapSystemId = model.HandicapSystemId,
                Value = model.Value,
                EffectiveFrom = model.EffectiveFrom,
                Notes = model.Notes
            });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var clubId = await _clubService.GetClubId(clubInitials);
            model.HandicapSystemOptions = await _handicapService.GetHandicapSystemsAsync(clubId);
            return View(model);
        }
        return RedirectToAction("Edit", new { clubInitials, id = model.BoatClassId });
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> EditHandicapEntry(string clubInitials, Guid boatClassId, Guid handicapId)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var boatClass = await _classService.GetClass(boatClassId);
        var ratings = await _handicapService.GetClassHandicapsAsync(boatClassId);
        var rating = ratings.Single(h => h.Id == handicapId);
        var systems = await _handicapService.GetHandicapSystemsAsync(clubId);
        return View(new ClassHandicapViewModel
        {
            Id = rating.Id,
            BoatClassId = boatClassId,
            HandicapSystemId = rating.HandicapSystemId,
            Value = rating.Value,
            EffectiveFrom = rating.EffectiveFrom,
            Notes = rating.Notes,
            ClassName = boatClass.Name,
            ClubInitials = clubInitials,
            HandicapSystemOptions = systems
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> EditHandicapEntry(string clubInitials, ClassHandicapViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            model.HandicapSystemOptions = await _handicapService.GetHandicapSystemsAsync(clubId);
            return View(model);
        }
        try
        {
            await _handicapService.SaveClassHandicapAsync(new ClassHandicap
            {
                Id = model.Id,
                BoatClassId = model.BoatClassId,
                HandicapSystemId = model.HandicapSystemId,
                Value = model.Value,
                EffectiveFrom = model.EffectiveFrom,
                Notes = model.Notes
            });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var clubId = await _clubService.GetClubId(clubInitials);
            model.HandicapSystemOptions = await _handicapService.GetHandicapSystemsAsync(clubId);
            return View(model);
        }
        return RedirectToAction("Edit", new { clubInitials, id = model.BoatClassId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> DeleteHandicap(string clubInitials, Guid handicapId, Guid boatClassId)
    {
        await _handicapService.DeleteClassHandicapAsync(handicapId);
        return RedirectToAction("Edit", new { clubInitials, id = boatClassId });
    }

    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Delete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var boatClass = await _classService.GetClassDeleteViewModel(id);
        if (!await _authService.IsUserClubAdministrator(User, clubId)
            || boatClass.ClubId != clubId)
        {
            return Unauthorized();
        }
        return View(boatClass);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var boatClass = await _classService.GetClassDeleteViewModel(id);
        if (boatClass.ClubId != clubId
            || !boatClass.IsDeletable)
        {
            return Unauthorized();
        }
        try
        {
            await _classService.Delete(id);

            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            return View(boatClass);
        }
    }
}
