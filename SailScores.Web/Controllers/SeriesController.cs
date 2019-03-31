using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    public class SeriesController : Controller
    {

        private readonly Web.Services.ISeriesService _seriesService;
        private readonly IClubService _clubService;
        private readonly Services.IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public SeriesController(
            Web.Services.ISeriesService seriesService,
            IClubService clubService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _seriesService = seriesService;
            _clubService = clubService;
            _authService = authService;
            _mapper = mapper;
        }

        // GET: Series
        public async Task<ActionResult> Index(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;

            var series = await _seriesService.GetAllSeriesSummaryAsync(clubInitials);

            return View(new ClubCollectionViewModel<SeriesSummary>
            {
                List = series,
                ClubInitials = clubInitials
            });
        }

        public async Task<ActionResult> Details(
            string clubInitials,
            string season,
            string seriesName)
        {
            ViewData["ClubInitials"] = clubInitials;

            var series = await _seriesService.GetSeriesAsync(clubInitials, season, seriesName);

            return View(new ClubItemViewModel<Core.Model.Series>
            {
                Item = series,
                ClubInitials = clubInitials
            });
        }


        [Authorize]
        public async Task<ActionResult> Create(string clubInitials)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            var vm = new SeriesWithOptionsViewModel();
            vm.SeasonOptions = club.Seasons;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Create(string clubInitials, SeriesWithOptionsViewModel model)
        {
            try
            {
                var club = (await _clubService.GetClubs(true)).Single(c =>
                    c.Initials.ToUpperInvariant() == clubInitials.ToUpperInvariant());
                if (!await _authService.CanUserEdit(User, club.Id))
                {
                    return Unauthorized();
                }
                model.ClubId = club.Id;
                await _seriesService.SaveNew(model);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View();
            }
        }

        [Authorize]
        public async Task<ActionResult> Edit(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id))
            {
                return Unauthorized();
            }
            var series =
                club.Series
                .SingleOrDefault(c => c.Id == id);
            if (series == null)
            {
                return NotFound();
            }
            var seriesWithOptions = _mapper.Map<SeriesWithOptionsViewModel>(series);
            seriesWithOptions.SeasonOptions = club.Seasons;
            return View(seriesWithOptions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Edit(string clubInitials, SeriesWithOptionsViewModel model)
        {
            try
            {
                var club = await _clubService.GetFullClub(clubInitials);
                if (!await _authService.CanUserEdit(User, club.Id)
                    || !club.Series.Any(c => c.Id == model.Id))
                {
                    return Unauthorized();
                }
                await _seriesService.Update(model);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View(model);
            }
        }

        [Authorize]
        public async Task<ActionResult> Delete(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id)
                || !club.Series.Any(c => c.Id == id))
            {
                return Unauthorized();
            }
            var series = club.Series.SingleOrDefault(c => c.Id == id);
            if (series == null)
            {
                return NotFound();
            }
            return View(series);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            if (!await _authService.CanUserEdit(User, club.Id)
                || !club.Series.Any(c => c.Id == id))
            {
                return Unauthorized();
            }
            try
            {
                await _seriesService.DeleteAsync(id);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                return View();
            }
        }

    }
}