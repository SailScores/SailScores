using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Core.Services;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class BoatClassController : Controller
{

    private readonly IClubService _clubService;
    private readonly IBoatClassService _classService;
    private readonly IAuthorizationService _authService;

    public BoatClassController(
        IClubService clubService,
        IBoatClassService classService,
        IAuthorizationService authService)
    {
        _clubService = clubService;
        _classService = classService;
        _authService = authService;
    }

    public ActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(
        string clubInitials,
        BoatClass model)
    {
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Forbid();
            }
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

    public async Task<ActionResult> Edit(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }
        var boatClass = await _classService.GetClass(id);
        return View(boatClass);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(
        string clubInitials,
        BoatClass model)
    {
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            var boatClass = await _classService.GetClass(model.Id);
            if (!await _authService.CanUserEdit(User, clubId)
                || boatClass.ClubId != clubId)
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

    public async Task<ActionResult> Delete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var boatClass = await _classService.GetClass(id);
        if (!await _authService.CanUserEdit(User, clubId)
            || boatClass.ClubId != clubId)
        {
            return Unauthorized();
        }
        //todo: add blocker if class contains boats. (or way to move boats.)
        return View(boatClass);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var boatClass = await _classService.GetClass(id);
        if (!await _authService.CanUserEdit(User, clubId)
            || boatClass.ClubId != clubId)
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