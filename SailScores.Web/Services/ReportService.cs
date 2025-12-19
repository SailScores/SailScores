using System;
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
            var clubId = await _clubService.GetClubId(clubInitials);
            var clubName = await _clubService.GetClubName(clubInitials);
            var club = await _clubService.GetMinimalClub(clubId);

            var useAdvancedFeatures = club?.UseAdvancedFeatures ?? false;
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
                ClubName = clubName,
                StartDate = displayStartDate,
                EndDate = displayEndDate,
                WindSpeedUnits = club?.WeatherSettings?.WindSpeedUnits ?? "m/s",
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
            var clubId = await _clubService.GetClubId(clubInitials);
            var clubName = await _clubService.GetClubName(clubInitials);
            var club = await _clubService.GetMinimalClub(clubId);

            var useAdvancedFeatures = club?.UseAdvancedFeatures ?? false;

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
                ClubName = clubName,
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
            var clubId = await _clubService.GetClubId(clubInitials);
            var clubName = await _clubService.GetClubName(clubInitials);
            var club = await _clubService.GetMinimalClub(clubId);

            var useAdvancedFeatures = club?.UseAdvancedFeatures ?? false;
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
                ClubName = clubName,
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
    }
