using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Core.Services;

public class ReportService : IReportService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IConversionService _conversionService;
        private readonly IClubService _clubService;

        public ReportService(
            ISailScoresContext dbContext,
            IConversionService conversionService,
            IClubService clubService)
        {
            _dbContext = dbContext;
            _conversionService = conversionService;
            _clubService = clubService;
        }

        public async Task<IList<WindDataPoint>> GetWindDataAsync(
            Guid clubId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _dbContext.Races
                .Where(r => r.ClubId == clubId && r.Date.HasValue);

            if (startDate.HasValue)
            {
                query = query.Where(r => r.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.Date <= endDate.Value);
            }

            var racesWithWeather = await query
                .Include(r => r.Weather)
                .Where(r => r.Weather != null)
                .ToListAsync()
                .ConfigureAwait(false);

            // Get club's preferred wind speed units
            var club = await _clubService.GetMinimalClub(clubId);
            var windSpeedUnits = club?.WeatherSettings?.WindSpeedUnits ?? _conversionService.MeterPerSecond;

            var windData = racesWithWeather
                .GroupBy(r => r.Date.Value.Date)
                .Select(g => new WindDataPoint
                {
                    Date = g.Key,
                    WindSpeed = _conversionService.Convert(
                        g.Where(r => r.Weather.WindSpeedMeterPerSecond.HasValue)
                            .Average(r => r.Weather.WindSpeedMeterPerSecond),
                        _conversionService.MeterPerSecond,
                        windSpeedUnits),
                    WindDirection = g.Where(r => r.Weather.WindDirectionDegrees.HasValue)
                        .Average(r => r.Weather.WindDirectionDegrees),
                    RaceCount = g.Count()
                })
                .OrderBy(w => w.Date)
                .ToList();

            return windData;
        }

        public async Task<IList<SkipperStatistics>> GetSkipperStatisticsAsync(
            Guid clubId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _dbContext.Races
                .Where(r => r.ClubId == clubId && r.Date.HasValue);

            if (startDate.HasValue)
            {
                query = query.Where(r => r.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.Date <= endDate.Value);
            }

            var races = await query
                .Include(r => r.Fleet)
                .Include(r => r.Scores)
                    .ThenInclude(s => s.Competitor)
                .Include(r => r.SeriesRaces)
                    .ThenInclude(sr => sr.Series)
                        .ThenInclude(s => s.Season)
                .ToListAsync()
                .ConfigureAwait(false);

            // Filter out races from series marked as ExcludeFromCompetitorStats
            var includedRaces = races
                .Where(r => !r.SeriesRaces.Any(sr => sr.Series.ExcludeFromCompetitorStats == true))
                .ToList();

            // Get season info for races (use first series season if available)
            var raceSeasons = includedRaces.ToDictionary(
                r => r.Id,
                r => r.SeriesRaces.FirstOrDefault()?.Series?.Season?.Name ?? "Unknown"
            );

            var fleetSeasonRaceCounts = includedRaces
                .GroupBy(r => new { FleetName = r.Fleet?.Name ?? "Unknown", SeasonName = raceSeasons[r.Id] })
                .ToDictionary(g => (g.Key.FleetName, g.Key.SeasonName), g => g.Count());

            var competitorStats = includedRaces
                .SelectMany(r => r.Scores.Select(s => new
                {
                    Race = r,
                    Score = s,
                    FleetName = r.Fleet?.Name ?? "Unknown",
                    SeasonName = raceSeasons[r.Id]
                }))
                .Where(x => x.Score.Competitor != null)
                .GroupBy(x => new
                {
                    x.Score.Competitor.Id,
                    x.Score.Competitor.Name,
                    x.Score.Competitor.SailNumber,
                    x.FleetName,
                    x.SeasonName
                })
                .Select(g =>
                {
                    var totalFleetRaces = fleetSeasonRaceCounts.TryGetValue(
                        (g.Key.FleetName, g.Key.SeasonName), 
                        out var count) ? count : 0;

                    var racesParticipated = g.Select(x => x.Race.Id).Distinct().Count();

                    var boatsBeat = g
                        .Where(x => x.Score.Place.HasValue)
                        .SelectMany(x => x.Race.Scores
                            .Where(s => s.Competitor != null
                                && s.Competitor.Id != g.Key.Id
                                && s.Place.HasValue
                                && s.Place.Value > x.Score.Place.Value)
                            .Select(s => s.Competitor.Id))
                        .Distinct()
                        .Count();

                    return new SkipperStatistics
                    {
                        CompetitorId = g.Key.Id,
                        CompetitorName = g.Key.Name,
                        SailNumber = g.Key.SailNumber,
                        FleetName = g.Key.FleetName,
                        SeasonName = g.Key.SeasonName,
                        RacesParticipated = racesParticipated,
                        TotalFleetRaces = totalFleetRaces,
                        BoatsBeat = boatsBeat,
                        ParticipationPercentage = totalFleetRaces > 0
                            ? (decimal)racesParticipated / totalFleetRaces * 100
                            : 0
                    };
                })
                .OrderByDescending(s => s.RacesParticipated)
                .ToList();

            return competitorStats;
        }

        public async Task<IList<ParticipationMetric>> GetParticipationMetricsAsync(
            Guid clubId,
            string groupBy = "month",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _dbContext.Races
                .Where(r => r.ClubId == clubId && r.Date.HasValue);

            if (startDate.HasValue)
            {
                query = query.Where(r => r.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.Date <= endDate.Value);
            }

            var races = await query
                .Include(r => r.Fleet)
                .Include(r => r.Scores)
                    .ThenInclude(s => s.Competitor)
                .ToListAsync()
                .ConfigureAwait(false);

            var metrics = races
                .SelectMany(r => r.Scores
                    .Where(s => s.Competitor != null)
                    .Select(s => new
                    {
                        Race = r,
                        Score = s,
                        FleetName = r.Fleet?.Name ?? "Unknown"
                    }))
                .GroupBy(x => new
                {
                    Period = GetPeriodKey(x.Race.Date.Value, groupBy),
                    PeriodStart = GetPeriodStart(x.Race.Date.Value, groupBy),
                    x.FleetName
                })
                .Select(g => new ParticipationMetric
                {
                    Period = g.Key.Period,
                    PeriodStart = g.Key.PeriodStart,
                    FleetName = g.Key.FleetName,
                    DistinctSkippers = g.Select(x => x.Score.Competitor.Id).Distinct().Count()
                })
                .OrderBy(m => m.PeriodStart)
                .ThenBy(m => m.FleetName)
                .ToList();

            return metrics;
        }

        private string GetPeriodKey(DateTime date, string groupBy)
        {
            return groupBy.ToLower() switch
            {
                "day" => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "week" => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), // Use date for week label
                "month" => date.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                "year" => date.Year.ToString(CultureInfo.InvariantCulture),
                _ => date.ToString("MMM yyyy", CultureInfo.InvariantCulture),
            };
        }

        private DateTime GetPeriodStart(DateTime date, string groupBy)
        {
            return groupBy.ToLower() switch
            {
                "day" => date.Date,
                "week" => GetMondayOfWeek(date),
                "month" => new DateTime(date.Year, date.Month, 1),
                "year" => new DateTime(date.Year, 1, 1),
                _ => new DateTime(date.Year, date.Month, 1),
            };
        }

        private DateTime GetMondayOfWeek(DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            // Convert Sunday (0) to 7 for easier calculation
            if (dayOfWeek == 0) dayOfWeek = 7;
            // Monday is day 1, so subtract (dayOfWeek - 1) to get to Monday
            return date.Date.AddDays(-(dayOfWeek - 1));
        }
    }
