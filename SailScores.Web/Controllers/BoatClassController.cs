using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Web.Authorization;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class BoatClassController : Controller
{

    private readonly CoreServices.IClubService _clubService;
    private readonly IBoatClassService _classService;
    private readonly IAuthorizationService _authService;

    public BoatClassController(
        CoreServices.IClubService clubService,
        IBoatClassService classService,
        IAuthorizationService authService)
    {
        _clubService = clubService;
        _classService = classService;
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
        return View(boatClass);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Edit(
        string clubInitials,
        BoatClass model)
    {
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            var boatClass = await _classService.GetClass(model.Id);
            if (boatClass.ClubId != clubId)
            {
                return Unauthorized();
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _classService.Update(model);

            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            return View();
        }
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
