using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class ReportService : Interfaces.IReportService
    {
        private readonly CoreServices.IReportService _coreReportService;
        private readonly CoreServices.IClubService _clubService;

        public ReportService(
            CoreServices.IReportService coreReportService,
            CoreServices.IClubService clubService)
        {
            _coreReportService = coreReportService;
            _clubService = clubService;
        }

        public async Task<WindAnalysisViewModel> GetWindAnalysisAsync(
            string clubInitials,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var club = await _clubService.GetMinimalClub(clubInitials);
            var clubId = club.Id;

        var useAdvancedFeatures = club.UseAdvancedFeatures ?? false;
            var originalStartDate = startDate;

            // Enforce 60-day limit for non-advanced clubs
            if (!useAdvancedFeatures)
            {
                var sixtyDaysAgo = DateTime.Today.AddDays(-60);
                if (!startDate.HasValue || startDate.Value < sixtyDaysAgo)
                {
                    startDate = sixtyDaysAgo;
                }
            }

            var windData = await _coreReportService.GetWindDataAsync(clubId, startDate, endDate);
            
            // If no dates provided, get full range for display
            DateTime? displayStartDate = originalStartDate;
            DateTime? displayEndDate = endDate;
            if (!originalStartDate.HasValue && !endDate.HasValue && windData.Any())
            {
                displayStartDate = windData.Min(w => w.Date);
                displayEndDate = windData.Max(w => w.Date);
            }

            return new WindAnalysisViewModel
            {
                ClubInitials = clubInitials,
                ClubName = club.Name,
                StartDate = displayStartDate,
                EndDate = displayEndDate,
                WindSpeedUnits = club.WeatherSettings?.WindSpeedUnits ?? "m/s",
                UseAdvancedFeatures = useAdvancedFeatures,
                WindData = windData.Select(w => new WindDataItem
                {
                    Date = w.Date,
                    WindSpeed = w.WindSpeed,
                    WindDirection = w.WindDirection,
                    RaceCount = w.RaceCount
                }).ToList()
            };
        }

        public async Task<SkipperStatsViewModel> GetSkipperStatsAsync(
            string clubInitials,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var club = await _clubService.GetMinimalClub(clubInitials);
            var clubId = club.Id;

        var useAdvancedFeatures = club.UseAdvancedFeatures ?? false;

            // Enforce 60-day limit for non-advanced clubs
            if (!useAdvancedFeatures)
            {
                var sixtyDaysAgo = DateTime.Today.AddDays(-60);
                if (!startDate.HasValue || startDate.Value < sixtyDaysAgo)
                {
                    startDate = sixtyDaysAgo;
                }
            }

            var skipperStats = await _coreReportService.GetSkipperStatisticsAsync(clubId, startDate, endDate);

            return new SkipperStatsViewModel
            {
                ClubInitials = clubInitials,
                ClubName = club.Name,
                StartDate = startDate,
                EndDate = endDate,
                UseAdvancedFeatures = useAdvancedFeatures,
                SkipperStats = skipperStats.Select(s => new SkipperStatItem
                {
                    CompetitorId = s.CompetitorId,
                    CompetitorName = s.CompetitorName,
                    SailNumber = s.SailNumber,
                    BoatClassName = s.BoatClassName,
                    SeasonName = s.SeasonName,
                    RacesParticipated = s.RacesParticipated,
                    TotalBoatClassRaces = s.TotalBoatClassRaces,
                    BoatsBeat = s.BoatsBeat,
                    ParticipationPercentage = s.ParticipationPercentage,
                    FirstRaceDate = s.FirstRaceDate,
                    LastRaceDate = s.LastRaceDate
                }).ToList()
            };
        }

        public async Task<ParticipationViewModel> GetParticipationAsync(
            string clubInitials,
            string groupBy = "month",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {

            var club = await _clubService.GetMinimalClub(clubInitials);
            var clubId = club.Id;

            var useAdvancedFeatures = club.UseAdvancedFeatures ?? false;
            var originalStartDate = startDate;

            // Enforce 90-day limit for non-advanced clubs
            if (!useAdvancedFeatures)
            {
                var ninetyDaysAgo = DateTime.Today.AddDays(-90);
                if (!startDate.HasValue || startDate.Value < ninetyDaysAgo)
                {
                    startDate = ninetyDaysAgo;
                }
            }

            var participationData = await _coreReportService.GetParticipationMetricsAsync(
                clubId, groupBy, startDate, endDate);
            
            // If no dates provided, get full range for display
            DateTime? displayStartDate = originalStartDate;
            DateTime? displayEndDate = endDate;
            if (!originalStartDate.HasValue && !endDate.HasValue && participationData.Any())
            {
                displayStartDate = participationData.Min(p => p.PeriodStart);
                displayEndDate = participationData.Max(p => p.PeriodStart);
            }

            return new ParticipationViewModel
            {
                ClubInitials = clubInitials,
                ClubName = club.Name,
                StartDate = displayStartDate,
                EndDate = displayEndDate,
                GroupBy = groupBy,
                UseAdvancedFeatures = useAdvancedFeatures,
                ParticipationData = participationData.Select(p => new ParticipationItem
                {
                    Period = p.Period,
                    PeriodStart = p.PeriodStart,
                    BoatClassName = p.BoatClassName,
                    DistinctSkippers = p.DistinctSkippers
                }).ToList()
            };
        }

        public async Task<AllCompHistogramViewModel> GetAllCompHistogramAsync(
            string clubInitials,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var club = await _clubService.GetMinimalClub(clubInitials);
            var clubId = club.Id;


            var useAdvancedFeatures = club.UseAdvancedFeatures ?? false;
            var originalStartDate = startDate;

            // Enforce 60-day limit for non-advanced clubs
            if (!useAdvancedFeatures)
            {
                var sixtyDaysAgo = DateTime.Today.AddDays(-60);
                if (!startDate.HasValue || startDate.Value < sixtyDaysAgo)
                {
                    startDate = sixtyDaysAgo;
                }
            }

            var histogram = await _coreReportService.GetAllCompHistogramStats(clubId, startDate, endDate);

            var codes = GetCodes(histogram.FieldList);
            var places = GetPlaces(histogram.FieldList);
            var rows = BuildRows(histogram.Stats, codes, places);

            var displayDates = await GetDisplayDatesAsync(clubId, originalStartDate, endDate, startDate);

            return new AllCompHistogramViewModel
            {
                ClubInitials = clubInitials,
                ClubName = club.Name,
                StartDate = displayDates.startDate,
                EndDate = displayDates.endDate,
                UseAdvancedFeatures = useAdvancedFeatures,
                Codes = codes,
                Places = places,
                Rows = rows
            };
        }


        private static List<string> GetCodes(IEnumerable<SailScores.Database.Entities.AllCompHistogramFields> fieldList)
        {
            return (fieldList ?? new List<SailScores.Database.Entities.AllCompHistogramFields>())
                .Select(f => f.Code)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();
        }

        private static List<int> GetPlaces(IEnumerable<SailScores.Database.Entities.AllCompHistogramFields> fieldList)
        {
            var maxPlace = (fieldList ?? new List<SailScores.Database.Entities.AllCompHistogramFields>())
                .Where(f => f.MaxPlace.HasValue)
                .Select(f => f.MaxPlace!.Value)
                .DefaultIfEmpty(0)
                .Max();

            return maxPlace > 0
                ? Enumerable.Range(1, maxPlace).ToList()
                : new List<int>();
        }

        private List<AllCompHistogramRow> BuildRows(IEnumerable<SailScores.Database.Entities.AllCompHistogramStats> stats, List<string> codes, List<int> places)
        {
            var statsList = stats ?? new List<SailScores.Database.Entities.AllCompHistogramStats>();

            return statsList
                .GroupBy(s => new { s.CompetitorId, s.CompetitorName, s.SailNumber, s.SeasonName, s.AggregationType })
                .OrderBy(g => g.Key.CompetitorName)
                .ThenBy(g => g.Key.SailNumber)
                .ThenBy(g => g.Key.SeasonName)
                .ThenBy(g => g.Key.AggregationType)
                .Select(g => BuildRow(g, codes, places))
                .ToList();
        }

        private AllCompHistogramRow BuildRow(IGrouping<dynamic, SailScores.Database.Entities.AllCompHistogramStats> group, List<string> codes, List<int> places)
        {
            var row = new AllCompHistogramRow
            {
                CompetitorName = group.Key.CompetitorName,
                SailNumber = group.Key.SailNumber,
                SeasonName = group.Key.SeasonName,
                AggregationType = group.Key.AggregationType,
            };

            foreach (var code in codes)
            {
                row.CodeCounts[code] = null;
            }

            foreach (var place in places)
            {
                row.PlaceCounts[place] = null;
            }

            foreach (var item in group)
            {
                if (!string.IsNullOrWhiteSpace(item.Code) && row.CodeCounts.ContainsKey(item.Code))
                {
                    row.CodeCounts[item.Code] = (row.CodeCounts[item.Code] ?? 0) + item.CountOfDistinct;
                }

                if (item.Place.HasValue && row.PlaceCounts.ContainsKey(item.Place.Value))
                {
                    row.PlaceCounts[item.Place.Value] = (row.PlaceCounts[item.Place.Value] ?? 0) + item.CountOfDistinct;
                }
            }

            return row;
        }

        private async Task<(DateTime? startDate, DateTime? endDate)> GetDisplayDatesAsync(Guid clubId, DateTime? originalStartDate, DateTime? endDate, DateTime? adjustedStartDate)
        {
            DateTime? displayStartDate = originalStartDate ?? adjustedStartDate;
            DateTime? displayEndDate = endDate;
            if (!originalStartDate.HasValue && !endDate.HasValue)
            {
                var skipperStats = await _coreReportService.GetSkipperStatisticsAsync(clubId, null, null);
                if (skipperStats != null && skipperStats.Any())
                {
                    var validStats = skipperStats.Where(s => s.FirstRaceDate.HasValue && s.LastRaceDate.HasValue);
                    if (validStats.Any())
                    {
                        displayStartDate = validStats.Min(s => s.FirstRaceDate.Value);
                        displayEndDate = validStats.Max(s => s.LastRaceDate.Value);
                    }
                }
            }
            return (displayStartDate, displayEndDate);
        }

        public string BuildAllCompPlacesCsv(AllCompHistogramViewModel model)
        {
            var csv = new System.Text.StringBuilder();

            csv.AppendLine(BuildCsvHeader(model));

            if (model.Rows != null)
            {
                foreach (var row in model.Rows)
                {
                    csv.AppendLine(BuildCsvRow(row, model));
                }
            }

            return csv.ToString();
        }

        private static string BuildCsvHeader(AllCompHistogramViewModel model)
        {
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

            return header;
        }

        private static string BuildCsvRow(AllCompHistogramRow row, AllCompHistogramViewModel model)
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

            return line.ToString();
        }
    }
