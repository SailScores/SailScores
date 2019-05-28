using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    public class RaceController : Controller
    {

        private readonly Core.Services.IClubService _clubService;
        private readonly Web.Services.IRaceService _raceService;
        private readonly Services.IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public RaceController(
            Core.Services.IClubService clubService,
            Web.Services.IRaceService raceService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _raceService = raceService;
            _authService = authService;
            _mapper = mapper;
        }

        public async Task<ActionResult> Index(string clubInitials,
            bool showScheduled = false,
            bool showAbandoned = true)
        {
            var races = await _raceService.GetAllRaceSummariesAsync(clubInitials,
                showScheduled,
                showAbandoned);

            return View(new ClubCollectionViewModel<RaceSummaryViewModel>
            {
                List = races,
                ClubInitials = clubInitials
            });
        }

        public async Task<ActionResult> Details(string clubInitials, Guid id)
        {
            var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);

            if(race == null)
            {
                return NotFound();
            }
            var canEdit = false;
            if(User != null && (User.Identity?.IsAuthenticated ?? false))
            {

                var club = (await _clubService.GetClubs(true)).Single(c =>
                    c.Initials.ToUpperInvariant() == clubInitials.ToUpperInvariant());
                canEdit = await _authService.CanUserEdit(User, club.Id);
            }

            return View(new ClubItemViewModel<RaceViewModel>
            {
                Item = race,
                ClubInitials = clubInitials,
                CanEdit = canEdit
            });
        }

        [Authorize]
        public async Task<ActionResult> Create(string clubInitials)
        {
            RaceWithOptionsViewModel race = await _raceService.GetBlankRaceWithOptions(clubInitials);

            return View(race);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Create(string clubInitials, RaceWithOptionsViewModel race)
        {
            try
            {
                var club = (await _clubService.GetClubs(true)).Single(c =>
                    c.Initials.ToUpperInvariant() == clubInitials.ToUpperInvariant());
                if (!await _authService.CanUserEdit(User, club.Id))
                {
                    return Unauthorized();
                }
                race.ClubId = club.Id;
                await _raceService.SaveAsync(race);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View();
            }
        }

        [Authorize]
        public async Task<ActionResult> Edit(
            string clubInitials,
            Guid id,
            string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);
            if (race == null)
            {
                return NotFound();
            }
            if (race.ClubId != club.Id)
            {
                return Unauthorized();
            }
            var raceWithOptions = _mapper.Map<RaceWithOptionsViewModel>(race);

            await _raceService.AddOptionsToRace(raceWithOptions);

            return View(raceWithOptions);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            RaceWithOptionsViewModel race,
            string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            try
            {
                if (!await _authService.CanUserEdit(User, race.ClubId))
                {
                    return Unauthorized();
                }
                await _raceService.SaveAsync(race);

                return RedirectToLocal(returnUrl);
            }
            catch
            {
                throw;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            var club = (await _clubService.GetClubs(true)).Single(c =>
                c.Initials.ToUpperInvariant() == clubInitials.ToUpperInvariant());
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);
            if (race == null)
            {
                return NotFound();
            }
            if (race.ClubId != club.Id)
            {
                return Unauthorized();
            }
            return View(race);
        }

        [Authorize]
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
        {
            try
            {
                var club = await _clubService.GetFullClub(clubInitials);
                if (!await _authService.CanUserEdit(User, club.Id)
                    || !club.Races.Any(c => c.Id == id))
                {
                    return Unauthorized();
                }
                await _raceService.Delete(id);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View();
            }
        }
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(AdminController.Index), "Admin");
            }
        }

    }
}