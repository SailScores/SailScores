using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
        private readonly Core.Services.IClubService _clubService;
        private readonly Services.IAuthorizationService _authService;
        private readonly IScoringService _scoringService;
        private readonly Services.IAdminTipService _adminTipService;
        private readonly ICsvService _csvService;
        private readonly IMapper _mapper;

        public SeriesController(
            Web.Services.ISeriesService seriesService,
            Core.Services.IClubService clubService,
            Services.IAuthorizationService authService,
            IScoringService scoringService,
            Services.IAdminTipService adminTipService,
            Services.ICsvService csvService,
            IMapper mapper)
        {
            _seriesService = seriesService;
            _clubService = clubService;
            _authService = authService;
            _scoringService = scoringService;
            _adminTipService = adminTipService;
            _csvService = csvService;
            _mapper = mapper;
        }

        // GET: Series
        public async Task<ActionResult> Index(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;

            var series = await _seriesService.GetNonRegattaSeriesSummariesAsync(clubInitials);

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
            if(series == null)
            {
                return new NotFoundResult();
            }
            var canEdit = false;
            if (User != null && (User.Identity?.IsAuthenticated ?? false))
            {
                var clubId = await _clubService.GetClubId(clubInitials);
                canEdit = await _authService.CanUserEdit(User, clubId);
            }

            return View(new ClubItemViewModel<Core.Model.Series>
            {
                Item = series,
                ClubInitials = clubInitials,
                CanEdit = canEdit
            });
        }

        public async Task<ActionResult> ExportCsv(
            string id)
        {

            var series = await _seriesService.GetSeriesAsync(new Guid(id));
            if (series == null)
            {
                return new NotFoundResult();
            }

            var filename = series.Name.Contains(series.Season.Name) ? $"{series.Name}.csv" : $"{series.Season.Name} {series.Name}.csv";
            var csv = _csvService.GetCsv(series);

            return File(csv, "text/csv", filename);
        }

        public async Task<ActionResult> ExportHtml(
            string id)
        {
            var series = await _seriesService.GetSeriesAsync(new Guid(id));
            if (series == null)
            {
                return new NotFoundResult();
            }
            var filename = series.Name.Contains(series.Season.Name) ? series.Name : $"{series.Season.Name} {series.Name}";
            // urlencode helps with unicode values, but replaces (valid) spaces.
            filename = HttpUtility.UrlEncode(filename + ".html", Encoding.UTF8);
            filename = filename.Replace("+", " ");
            var disposition = $"attachment; filename=\"{filename}\"; filename*=UTF-8''{filename}";
            Response.Headers.Add("content-disposition", disposition);

            return View(series);
        }

        public async Task<JsonResult> Chart(
            Guid seriesId)
        {
            var chartData = await _seriesService.GetChartData(seriesId);
            
            return Json(chartData);
        }


        [Authorize]
        public async Task<ActionResult> Create(string clubInitials)
        {
            var club = await _clubService.GetFullClub(clubInitials);
            var vm = new SeriesWithOptionsViewModel
            {
                SeasonOptions = club.Seasons
            };
            var scoringSystemOptions = await _scoringService.GetScoringSystemsAsync(club.Id, true);
            scoringSystemOptions.Add(new ScoringSystem
            {
                Id = Guid.Empty,
                Name = "<Use Club Default>"
            });
            vm.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();

            var errors = _adminTipService.GetSeriesCreateErrors(vm);
            if (errors != null && errors.Count > 0)
            {
                return View("CreateErrors", errors);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> Create(string clubInitials, SeriesWithOptionsViewModel model)
        {
            try
            {
                var clubId = await _clubService.GetClubId(clubInitials);
                if (!await _authService.CanUserEdit(User, clubId))
                {
                    return Unauthorized();
                }
                model.ClubId = clubId;

                if (!ModelState.IsValid)
                {
                    var club = await _clubService.GetFullClub(clubInitials);
                    model.SeasonOptions = club.Seasons;
                    var scoringSystemOptions = await _scoringService.GetScoringSystemsAsync(club.Id, true);
                    scoringSystemOptions.Add(new ScoringSystem
                    {
                        Id = Guid.Empty,
                        Name = "<Use Club Default>"
                    });
                    model.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();
                    return View(model);
                }
                await _seriesService.SaveNew(model);

                return RedirectToAction("Index", "Admin");
            }
            catch
            {
                var club = await _clubService.GetFullClub(clubInitials);
                model.SeasonOptions = club.Seasons;
                var scoringSystemOptions = await _scoringService.GetScoringSystemsAsync(club.Id, true);
                scoringSystemOptions.Add(new ScoringSystem
                {
                    Id = Guid.Empty,
                    Name = "<Use Club Default>"
                });
                model.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();
                return View(model);
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

            var scoringSystemOptions = await _scoringService.GetScoringSystemsAsync(club.Id, true);
            scoringSystemOptions.Add(new ScoringSystem
            {
                Id = Guid.Empty,
                Name = "<Use Club Default>"
            });
            seriesWithOptions.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();
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

                if (!ModelState.IsValid)
                {
                    model.SeasonOptions = club.Seasons;
                    var scoringSystemOptions = await _scoringService.GetScoringSystemsAsync(club.Id, true);
                    scoringSystemOptions.Add(new ScoringSystem
                    {
                        Id = Guid.Empty,
                        Name = "<Use Club Default>"
                    });
                    model.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();
                    return View(model);
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