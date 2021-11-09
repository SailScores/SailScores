﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SailScores.Identity.Entities;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

public class UserController : Controller
{
    private readonly IClubService _clubService;
    private readonly IPermissionService _permissionService;
    private readonly IMapper _mapper;
    private readonly IAuthorizationService _authService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserController(
        IClubService clubService,
        IPermissionService permissionService,
        IAuthorizationService authService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _clubService = clubService;
        _permissionService = permissionService;
        _authService = authService;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<ActionResult> Add(
        string clubInitials,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ClubInitials"] = clubInitials.ToUpperInvariant();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Add(
        string clubInitials,
        UserViewModel model,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.CreatedBy = await GetUserStringAsync();
            model.Created = DateTime.UtcNow;
            await _permissionService.UpdatePermission(clubId, model);

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect(Url.RouteUrl(new {
                controller = "Admin",
                action = "Index"}) + "#scorekeepers");

        }
        catch
        {
            ModelState.AddModelError(String.Empty, "An error has occurred.");
            return View(model);
        }
    }

    [Authorize]
    public async Task<ActionResult> Delete(string clubInitials, Guid id)
    {
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }
        var user = await _userManager.GetUserAsync(User);
        if (!(await _permissionService.CanDelete(user.Email, id)))
        {
            return RedirectToAction("Error", "User");
        }
        var permission = await _permissionService.GetUserAsync(id);
        return View(permission);
    }

    [Authorize]
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> PostDelete(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }
        try
        {
            await _permissionService.Delete(id);

            if (!String.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect(Url.RouteUrl(new
            {
                controller = "Admin",
                action = "Index"
            }) + "#scorekeepers");
        }
        catch
        {
            var permission = await _permissionService.GetUserAsync(id);
            return View(permission);
        }
    }
    public async Task<ActionResult> Error(string clubInitials)
    {
        return View();
    }

    private async Task<string> GetUserStringAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user.GetDisplayName();
    }
}