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

        public async Task<ActionResult> CompetitorStats(
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

        public async Task<ActionResult> WindAnalysisExport(
            string clubInitials,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }

            var model = await _reportService.GetWindAnalysisAsync(clubInitials, startDate, endDate);
            
            var csv = new System.Text.StringBuilder();
            csv.AppendLine($"Date,Wind Speed ({model.WindSpeedUnits}),Wind Direction (degrees),Race Count");
            
            foreach (var item in model.WindData)
            {
                if(item.WindSpeed == null || item.WindDirection == null)
                {
                    continue; // Skip entries with missing data
                }

                // Round wind speed to 1 decimal place, direction to whole degrees
                csv.AppendLine($"{item.Date:yyyy-MM-dd},{item.WindSpeed:F1},{Math.Round(item.WindDirection.Value)},{item.RaceCount}");
            }
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"{clubInitials}_WindAnalysis_{DateTime.Now:yyyyMMdd}.csv";
            return File(bytes, "text/csv", fileName);
        }

        public async Task<ActionResult> ParticipationExport(
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
            
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Period,Fleet,Distinct Skippers");
            
            foreach (var item in model.ParticipationData)
            {
                csv.AppendLine($"{item.Period},{item.FleetName},{item.DistinctSkippers}");
            }
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"{clubInitials}_Participation_{groupBy}_{DateTime.Now:yyyyMMdd}.csv";
            return File(bytes, "text/csv", fileName);
        }

        public async Task<ActionResult> CompetitorStatsExport(
            string clubInitials,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }

            var model = await _reportService.GetSkipperStatsAsync(clubInitials, startDate, endDate);
            
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Competitor,Sail Number,Fleet,Season,Races Participated,Total Fleet Races,Boats Beat,Participation %");
            
            foreach (var item in model.SkipperStats)
            {
                csv.AppendLine($"\"{item.CompetitorName}\",{item.SailNumber},{item.FleetName},{item.SeasonName},{item.RacesParticipated},{item.TotalFleetRaces},{item.BoatsBeat},{item.ParticipationPercentage:F1}");
            }
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"{clubInitials}_CompetitorStats_{DateTime.Now:yyyyMMdd}.csv";
            return File(bytes, "text/csv", fileName);
        }
    }
