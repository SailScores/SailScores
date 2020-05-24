using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Core.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Controllers
{
    [Authorize]
    public class SeasonController : Controller
    {

        private readonly IClubService _clubService;
        private readonly ISeasonService _seasonService;
        private readonly Services.IAuthorizationService _authService;

        public SeasonController(
            IClubService clubService,
            ISeasonService seasonService,
            Services.IAuthorizationService authService)
        {
            _clubService = clubService;
            _seasonService = seasonService;
            _authService = authService;
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(string clubInitials, Season model)
        {
            try
            {
                var club = (await _clubService.GetFullClub(clubInitials));
                if (!await _authService.CanUserEdit(User, club.Id))
                {
                    return Unauthorized();
                }
                model.ClubId = club.Id;
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                await _seasonService.SaveNew(model);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View();
            }
        }

        public async Task< ActionResult> Edit(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            var season =
                club.Seasons
                .SingleOrDefault(c => c.Id == id);
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
                var club = await _clubService.GetFullClub(clubInitials);
                if (!await _authService.CanUserEdit(User, club.Id)
                    || !club.Seasons.Any(c => c.Id == model.Id))
                {
                    return Unauthorized();
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
                return View();
            }
        }

        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id)
                || !club.Seasons.Any(c => c.Id == id))
            {
                return Unauthorized();
            }
            var season = club.Seasons.SingleOrDefault(c => c.Id == id);
            if (season == null)
            {
                return NotFound();
            }
            //todo: add blocker if class contains boats. (or way to move boats.)
            return View(season);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id)
                || !club.Seasons.Any(c => c.Id == id))
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
                return View();
            }
        }
    }
}