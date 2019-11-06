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
            bool includeRegattaSeries);
        Task<Model.Series> GetOneSeriesAsync(Guid seriesId);
        Task SaveNewSeries(Series series, Club club);
        Task SaveNewSeries(Series series);
        Task Update(Series model);
        Task Delete(Guid fleetId);
        Task UpdateSeriesResults(Guid seriesId);
        Task<FlatModel.FlatResults> GetHistoricalResults(Series series);
        Task<FlatModel.FlatChartData> GetChartData(Guid seriesId);
    }
}