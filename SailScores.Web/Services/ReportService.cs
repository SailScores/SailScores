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

            var windData = await _coreReportService.GetWindDataAsync(clubId, startDate, endDate);

            return new WindAnalysisViewModel
            {
                ClubInitials = clubInitials,
                ClubName = clubName,
                StartDate = startDate,
                EndDate = endDate,
                WindSpeedUnits = club?.WeatherSettings?.WindSpeedUnits ?? "m/s",
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

            var skipperStats = await _coreReportService.GetSkipperStatisticsAsync(clubId, startDate, endDate);

            return new SkipperStatsViewModel
            {
                ClubInitials = clubInitials,
                ClubName = clubName,
                StartDate = startDate,
                EndDate = endDate,
                SkipperStats = skipperStats.Select(s => new SkipperStatItem
                {
                    CompetitorId = s.CompetitorId,
                    CompetitorName = s.CompetitorName,
                    SailNumber = s.SailNumber,
                    FleetName = s.FleetName,
                    RacesParticipated = s.RacesParticipated,
                    TotalFleetRaces = s.TotalFleetRaces,
                    BoatsBeat = s.BoatsBeat,
                    ParticipationPercentage = s.ParticipationPercentage
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

            var participationData = await _coreReportService.GetParticipationMetricsAsync(
                clubId, groupBy, startDate, endDate);

            return new ParticipationViewModel
            {
                ClubInitials = clubInitials,
                ClubName = clubName,
                StartDate = startDate,
                EndDate = endDate,
                GroupBy = groupBy,
                ParticipationData = participationData.Select(p => new ParticipationItem
                {
                    Period = p.Period,
                    PeriodStart = p.PeriodStart,
                    FleetName = p.FleetName,
                    DistinctSkippers = p.DistinctSkippers
                }).ToList()
            };
        }
    }
