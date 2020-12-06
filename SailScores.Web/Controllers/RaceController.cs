using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SailScores.Identity.Entities;

namespace SailScores.Web.Controllers
{
    public class RaceController : Controller
    {

        private readonly Core.Services.IClubService _clubService;
        private readonly Web.Services.IRaceService _raceService;
        private readonly Services.IAuthorizationService _authService;
        private readonly Services.IAdminTipService _adminTipService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public RaceController(
            Core.Services.IClubService clubService,
            Web.Services.IRaceService raceService,
            Services.IAuthorizationService authService,
            Services.IAdminTipService adminTipService,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _clubService = clubService;
            _raceService = raceService;
            _authService = authService;
            _adminTipService = adminTipService;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<ActionResult> Index(
            string clubInitials,
            string seasonName,
            bool showScheduled = true,
            bool showAbandoned = true)
        {
            if (String.IsNullOrWhiteSpace(seasonName))
            {
                var currentSeason = await _raceService.GetCurrentSeasonAsync(clubInitials);
                if (currentSeason != null)
                {
                    return RedirectToRoute("Race", new
                    {
                        clubInitials,
                        seasonName = currentSeason.UrlName
                    });
                }
            }
            var races = await _raceService.GetAllRaceSummariesAsync(
                clubInitials,
                seasonName,
                showScheduled,
                showAbandoned);

            return View(new ClubItemViewModel<RaceSummaryListViewModel>
            {
                Item = races,
                ClubInitials = clubInitials,
                CanEdit = await _authService.CanUserEdit(User, clubInitials)
            });
        }

        public async Task<ActionResult> Details(string clubInitials, Guid id)
        {
            var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);

            if (race == null)
            {
                return NotFound();
            }
            var canEdit = false;
            if (User != null && (User.Identity?.IsAuthenticated ?? false))
            {
                canEdit = await _authService.CanUserEdit(User, clubInitials);
            }

            return View(new ClubItemViewModel<RaceViewModel>
            {
                Item = race,
                ClubInitials = clubInitials,
                CanEdit = canEdit
            });
        }

        [Authorize]
        public async Task<ActionResult> Create(
            string clubInitials,
            Guid? regattaId,
            Guid? seriesId,
            string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            RaceWithOptionsViewModel race =
                await _raceService.GetBlankRaceWithOptions(
                    clubInitials,
                    regattaId,
                    seriesId);
            var errors = _adminTipService.GetRaceCreateErrors(race);
            if (errors != null && errors.Count > 0)
            {
                return View("CreateErrors", errors);
            }
            _adminTipService.AddTips(ref race);
            return View(race);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Create(
            string clubInitials,
            RaceWithOptionsViewModel race,
            string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                RaceWithOptionsViewModel raceOptions =
                    await _raceService.GetBlankRaceWithOptions(
                        clubInitials,
                        race.RegattaId,
                        race.SeriesIds?.FirstOrDefault());
                race.ScoreCodeOptions = raceOptions.ScoreCodeOptions;
                race.FleetOptions = raceOptions.FleetOptions;
                race.CompetitorBoatClassOptions = raceOptions.CompetitorBoatClassOptions;
                race.CompetitorOptions = raceOptions.CompetitorOptions;
                race.SeriesOptions = raceOptions.SeriesOptions;
                race.WeatherIconOptions = raceOptions.WeatherIconOptions;
                return View(race);
            }
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }
            race.ClubId = clubId;
            race.UpdatedBy = await GetUserStringAsync();
            await _raceService.SaveAsync(race);
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");

        }

        private async Task<string> GetUserStringAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user.GetDisplayName();
        }

        [Authorize]
        public async Task<ActionResult> Edit(
            string clubInitials,
            Guid id,
            string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["ClubInitials"] = clubInitials;
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }
            var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);
            if (race == null)
            {
                return NotFound();
            }
            if (race.ClubId != clubId)
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
            string clubInitials,
            Guid id,
            RaceWithOptionsViewModel race,
            string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["ClubInitials"] = clubInitials;

            if (!await _authService.CanUserEdit(User, race.ClubId))
            {
                return Unauthorized();
            }
            if (!ModelState.IsValid)
            {
                RaceWithOptionsViewModel raceOptions =
                    await _raceService.GetBlankRaceWithOptions(
                        clubInitials,
                        race.RegattaId,
                        race.SeriesIds?.FirstOrDefault());
                race.ScoreCodeOptions = raceOptions.ScoreCodeOptions;
                race.FleetOptions = raceOptions.FleetOptions;
                race.CompetitorBoatClassOptions = raceOptions.CompetitorBoatClassOptions;
                race.CompetitorOptions = raceOptions.CompetitorOptions;
                race.SeriesOptions = raceOptions.SeriesOptions;
                foreach (var score in race.Scores)
                {
                    score.Competitor = raceOptions.CompetitorOptions.First(c => c.Id == score.CompetitorId);
                }
                return View(race);
            }

            race.UpdatedBy = await GetUserStringAsync();
            await _raceService.SaveAsync(race);

            return RedirectToLocal(returnUrl);
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }
            var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);
            if (race == null)
            {
                return NotFound();
            }
            if (race.ClubId != clubId)
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
                var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);
                if (!await _authService.CanUserEdit(User, clubInitials)
                    || race == null)
                {
                    return Unauthorized();
                }
                await _raceService.Delete(id, await GetUserStringAsync());

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