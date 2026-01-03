using SailScores.Web.Models.SailScores;
using Microsoft.AspNetCore.Http;

namespace SailScores.Web.Services.Interfaces;

public interface ISeriesService
{
    Task<IEnumerable<SeriesSummary>> GetNonRegattaSeriesSummariesAsync(string clubInitials);
    Task<IEnumerable<SeriesSummary>> GetChildSeriesSummariesAsync(
        Guid clubId,
        Guid seasonId);
    Task<IEnumerable<SeriesSummary>> GetSummarySeriesAsync(Guid clubId);
    Task<Core.Model.Series> GetSeriesAsync(string clubInitials, string season, string seriesUrlName);
    Task<Core.Model.Series> GetSeriesAsync(Guid seriesId);
    Task<Guid> SaveNew(SeriesWithOptionsViewModel model);
    Task Update(SeriesWithOptionsViewModel model);
    Task DeleteAsync(Guid id);
    Task<Core.FlatModel.FlatChartData> GetChartData(Guid seriesId);
    Task<SeriesWithOptionsViewModel> GetBlankVmForCreate(string clubInitials);
    Task<MultipleSeriesWithOptionsViewModel> GetBlankVmForCreateMultiple(string clubInitials);
    Task<IList<Guid>> CreateMultipleAsync(string clubInitials, Guid clubId, MultipleSeriesWithOptionsViewModel model, string updatedBy);
    Task<IEnumerable<string>> GetSeriesNamesAsync(Guid clubId, Guid seasonId);
    Task<IcalImportResult> ImportIcalAsync(string clubInitials, Guid seasonId, IFormFile file, string url);
}
