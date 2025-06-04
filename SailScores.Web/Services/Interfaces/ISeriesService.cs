using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface ISeriesService
{
    Task<IEnumerable<SeriesSummary>> GetNonRegattaSeriesSummariesAsync(string clubInitials);
    Task<IEnumerable<SeriesSummary>> GetChildSeriesSummariesAsync(
        Guid clubId,
        Guid seasonId);
    Task<Core.Model.Series> GetSeriesAsync(string clubInitials, string season, string seriesUrlName);
    Task<Core.Model.Series> GetSeriesAsync(Guid seriesId);
    Task SaveNew(SeriesWithOptionsViewModel model);
    Task Update(SeriesWithOptionsViewModel model);
    Task DeleteAsync(Guid id);
    Task<Core.FlatModel.FlatChartData> GetChartData(Guid seriesId);
    Task<SeriesWithOptionsViewModel> GetBlankVmForCreate(string clubInitials);
}