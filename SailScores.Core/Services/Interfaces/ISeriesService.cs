using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface ISeriesService
    {
        Task<Core.Model.Series> GetSeriesDetailsAsync(
            string clubInitials,
            string seasonName,
            string seriesUrlName);

        Task<IList<Model.Series>> GetAllSeriesAsync(
            Guid clubId,
            DateTime? date,
            bool includeRegatta,
            bool includeSummary);

        Task<Model.Series> GetOneSeriesAsync(Guid seriesId);
        Task<Guid> SaveNewSeries(Series series, Club club);
        Task<Guid> SaveNewSeries(Series series);
        Task Update(Series model);
        Task Delete(Guid seriesId);
        Task UpdateSeriesResults(Guid seriesId, string updatedBy,
            bool calculateParents = true);
        Task UpdateParentSeriesResults(Guid seriesId, string updatedBy);
        Task<FlatModel.FlatResults> GetHistoricalResults(Series series);
        Task<FlatModel.FlatChartData> GetChartData(Guid seriesId);
        Task<Model.Series> CalculateWhatIfScoresAsync(
            Guid seriesId,
            Guid scoringSystemId,
            int discards,
            decimal? participationPercent);
    }
}