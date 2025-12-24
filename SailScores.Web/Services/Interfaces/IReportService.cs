using System;
using System.Threading.Tasks;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IReportService
{
    Task<WindAnalysisViewModel> GetWindAnalysisAsync(
        string clubInitials,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<SkipperStatsViewModel> GetSkipperStatsAsync(
        string clubInitials,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<ParticipationViewModel> GetParticipationAsync(
        string clubInitials,
        string groupBy = "month",
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<AllCompHistogramViewModel> GetAllCompHistogramAsync(
        string clubInitials,
        DateTime? startDate = null,
        DateTime? endDate = null);
}
