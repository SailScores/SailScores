﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class SeasonController : Controller
{

    private readonly CoreServices.IClubService _clubService;
    private readonly ISeasonService _seasonService;
    private readonly IAuthorizationService _authService;

    public SeasonController(
        CoreServices.IClubService clubService,
        ISeasonService seasonService,
        IAuthorizationService authService)
    {
        _clubService = clubService;
        _seasonService = seasonService;
        _authService = authService;
    }

    public async Task<ActionResult> Create(string clubInitials)
    {

        var clubId = await _clubService.GetClubId(clubInitials);
        Season suggestion = await _seasonService.GetSeasonSuggestion(clubId);
        return View(suggestion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(string clubInitials, Season model)
    {
        try
        {
            var clubId = (await _clubService.GetClubId(clubInitials));
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }
            model.ClubId = clubId;

            var errors = await _seasonService.GetSavingSeasonErrors(model);
            foreach (var error in errors)
            {
                ModelState.AddModelError(String.Empty, error);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _seasonService.SaveNew(model);

            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            ModelState.AddModelError(String.Empty, "An error occurred saving these changes.");
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

        var season = (await _seasonService.GetSeasons(clubId))
            .SingleOrDefault(s => s.Id == id);
        if (season == null)
        {
            return NotFound();
        }
        return View(season);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(string clubInitials, Season model)
    {
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            var seasonFromDb = (await _seasonService.GetSeasons(clubId)
                ).FirstOrDefault(s => s.Id == model.Id);
            if (!await _authService.CanUserEdit(User, clubId)
                || seasonFromDb == null)
            {
                return Unauthorized();
            }
            var errors = await _seasonService.GetSavingSeasonErrors(model);
            foreach (var error in errors)
            {
                ModelState.AddModelError(String.Empty, error);
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            await _seasonService.Update(model);

            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            ModelState.AddModelError(String.Empty, "An error occurred saving these changes.");
            return View(model);
        }
    }

    public async Task<ActionResult> Delete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }

        var season = (await _seasonService.GetSeasons(clubId)
            ).FirstOrDefault(s => s.Id == id);
        if (season == null)
        {
            return NotFound();
        }
        //todo: add blocker if season has series or regattas
        return View(season);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var seasonFromDb = (await _seasonService.GetSeasons(clubId)
            ).FirstOrDefault(s => s.Id == id);
        if (!await _authService.CanUserEdit(User, clubId)
            || seasonFromDb == null)
        {
            return Unauthorized();
        }
        try
        {
            await _seasonService.Delete(id);
            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            ModelState.AddModelError(String.Empty, "An error occurred deleting this season. Is it in use?");
            return View(seasonFromDb);
        }
    }
}