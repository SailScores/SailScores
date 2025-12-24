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
    private readonly IClubService _webClubService;
    private readonly IAuthorizationService _authService;

    public ReportsController(
        IReportService reportService,
        CoreServices.IClubService clubService,
        IClubService webClubService,
        IAuthorizationService authService)
    {
        _reportService = reportService;
        _clubService = clubService;
        _webClubService = webClubService;
        _authService = authService;
    }

    public async Task<ActionResult> Index(string clubInitials)
    {
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }

        var clubId = await _clubService.GetClubId(clubInitials);
        var club = await _clubService.GetMinimalClub(clubId);
        var clubName = await _clubService.GetClubName(clubInitials);

        var model = new Models.SailScores.ReportsIndexViewModel
        {
            ClubInitials = clubInitials,
            ClubName = clubName,
            CanEdit = true,
            UseAdvancedFeatures = club?.UseAdvancedFeatures ?? false
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

    public async Task<ActionResult> ClubStats(string clubInitials)
    {
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }

        ViewData["ClubInitials"] = clubInitials;

        var stats = await _webClubService.GetClubStats(clubInitials);
        stats.CanEdit = true;
        return View(stats);
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
        csv.AppendLine("Period,Boat Class,Distinct Skippers");
        
        foreach (var item in model.ParticipationData)
        {
            csv.AppendLine($"{item.Period},{item.BoatClassName},{item.DistinctSkippers}");
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
        csv.AppendLine("Competitor,Sail Number,Boat Class,Season,Races Participated,Total Boat Class Races,Boats Beat,Participation %");
        
        foreach (var item in model.SkipperStats)
        {
            csv.AppendLine($"\"{item.CompetitorName}\",{item.SailNumber},{item.BoatClassName},{item.SeasonName},{item.RacesParticipated},{item.TotalBoatClassRaces},{item.BoatsBeat},{item.ParticipationPercentage:F1}");
        }
        
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"{clubInitials}_CompetitorStats_{DateTime.Now:yyyyMMdd}.csv";
        return File(bytes, "text/csv", fileName);
    }

    public async Task<ActionResult> ClubStatsExport(string clubInitials)
    {
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }

        var stats = await _webClubService.GetClubStats(clubInitials);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Season,Boat Class,Distinct Competitors,Races,Total Starts,Race Days,Average Competitors Per Race,First Race,Last Race");

        if (stats?.SeasonStats != null)
        {
            foreach (var item in stats.SeasonStats)
            {
                var season = item.SeasonName ?? string.Empty;
                var boatClass = item.ClassName ?? string.Empty;
                var distinctCompetitors = item.DistinctCompetitorsStarted?.ToString() ?? string.Empty;
                var races = item.RaceCount?.ToString() ?? string.Empty;
                var totalStarts = item.CompetitorsStarted?.ToString() ?? string.Empty;
                var raceDays = item.DistinctDaysRaced?.ToString() ?? string.Empty;
                var avg = item.AverageCompetitorsPerRace.HasValue ? item.AverageCompetitorsPerRace.Value.ToString("F1") : string.Empty;
                var firstRace = item.FirstRace.HasValue ? item.FirstRace.Value.ToString("yyyy-MM-dd") : string.Empty;
                var lastRace = item.LastRace.HasValue ? item.LastRace.Value.ToString("yyyy-MM-dd") : string.Empty;

                // Wrap season and boat class in quotes to be safe if they contain commas
                csv.AppendLine($"\"{season}\",\"{boatClass}\",{distinctCompetitors},{races},{totalStarts},{raceDays},{avg},{firstRace},{lastRace}");
            }
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"{clubInitials}_ClubStats_{DateTime.Now:yyyyMMdd}.csv";
        return File(bytes, "text/csv", fileName);
    }

    public async Task<ActionResult> AllCompPlaces(
        string clubInitials,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }

        var model = await _reportService.GetAllCompHistogramAsync(clubInitials, startDate, endDate);
        ViewData["ClubInitials"] = clubInitials;
        return View(model);
    }

    public async Task<ActionResult> AllCompPlacesExport(
        string clubInitials,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }

        var model = await _reportService.GetAllCompHistogramAsync(clubInitials, startDate, endDate);

        var csv = new System.Text.StringBuilder();

        // Header
        var header = "Competitor,Sail Number,Season,Aggregation";
        if (model.Places != null && model.Places.Any())
        {
            foreach (var p in model.Places)
            {
                header += "," + $"Place {p}";
            }
        }

        if (model.Codes != null && model.Codes.Any())
        {
            foreach (var c in model.Codes)
            {
                header += "," + c;
            }
        }

        csv.AppendLine(header);

        if (model.Rows != null)
        {
            foreach (var row in model.Rows)
            {
                var line = new System.Text.StringBuilder();
                var escapedName = (row.CompetitorName ?? string.Empty).Replace("\"", "\"\"");
                line.Append('"').Append(escapedName).Append('"');
                line.Append(",").Append(row.SailNumber ?? string.Empty);
                line.Append(",").Append(row.SeasonName ?? string.Empty);
                line.Append(",").Append(row.AggregationType ?? string.Empty);

                if (model.Places != null && model.Places.Any())
                {
                    foreach (var p in model.Places)
                    {
                        row.PlaceCounts.TryGetValue(p, out var pcount);
                        line.Append(",").Append(pcount?.ToString() ?? string.Empty);
                    }
                }

                if (model.Codes != null && model.Codes.Any())
                {
                    foreach (var c in model.Codes)
                    {
                        row.CodeCounts.TryGetValue(c, out var ccount);
                        line.Append(",").Append(ccount?.ToString() ?? string.Empty);
                    }
                }

                csv.AppendLine(line.ToString());
            }
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"{clubInitials}_AllCompetitorPlaces_{DateTime.Now:yyyyMMdd}.csv";
        return File(bytes, "text/csv", fileName);
    }
}
