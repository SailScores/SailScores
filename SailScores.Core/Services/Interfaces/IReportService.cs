using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Core.Services;

public interface IReportService
{
    Task<IList<WindDataPoint>> GetWindDataAsync(
        Guid clubId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<IList<SkipperStatistics>> GetSkipperStatisticsAsync(
        Guid clubId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<IList<ParticipationMetric>> GetParticipationMetricsAsync(
        Guid clubId,
        string groupBy = "month",
        DateTime? startDate = null,
        DateTime? endDate = null);
}

public class WindDataPoint
{
    public DateTime Date { get; set; }
    public decimal? WindSpeed { get; set; }
    public decimal? WindDirection { get; set; }
    public int RaceCount { get; set; }
}

public class SkipperStatistics
{
    public Guid CompetitorId { get; set; }
    public string CompetitorName { get; set; }
    public string SailNumber { get; set; }
    public string FleetName { get; set; }
    public string SeasonName { get; set; }
    public int RacesParticipated { get; set; }
    public int TotalFleetRaces { get; set; }
    public int BoatsBeat { get; set; }
    public decimal ParticipationPercentage { get; set; }
}

public class ParticipationMetric
{
    public string Period { get; set; }
    public DateTime PeriodStart { get; set; }
    public string FleetName { get; set; }
    public int DistinctSkippers { get; set; }
}
