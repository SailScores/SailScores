using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly CoreServices.IClubService _clubService;
        private readonly IAuthorizationService _authService;

        public ReportsController(
            IReportService reportService,
            CoreServices.IClubService clubService,
            IAuthorizationService authService)
        {
            _reportService = reportService;
            _clubService = clubService;
            _authService = authService;
        }

        public async Task<ActionResult> Index(string clubInitials)
        {
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }

            var clubName = await _clubService.GetClubName(clubInitials);

            var model = new Models.SailScores.ReportsIndexViewModel
            {
                ClubInitials = clubInitials,
                ClubName = clubName,
                CanEdit = true
            };

            ViewData["ClubInitials"] = clubInitials;
            return View(model);
        }

        public async Task<ActionResult> WindAnalysis(
            string clubInitials,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }

            var model = await _reportService.GetWindAnalysisAsync(clubInitials, startDate, endDate);
            model.CanEdit = true;

            ViewData["ClubInitials"] = clubInitials;
            return View(model);
        }

        public async Task<ActionResult> SkipperStats(
            string clubInitials,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }

            var model = await _reportService.GetSkipperStatsAsync(clubInitials, startDate, endDate);
            model.CanEdit = true;

            ViewData["ClubInitials"] = clubInitials;
            return View(model);
        }

        public async Task<ActionResult> Participation(
            string clubInitials,
            string groupBy = "month",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }

            var model = await _reportService.GetParticipationAsync(clubInitials, groupBy, startDate, endDate);
            model.CanEdit = true;

            ViewData["ClubInitials"] = clubInitials;
            return View(model);
        }
    }
