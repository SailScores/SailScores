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
            string seriesName);
        Task<IList<Model.Series>> GetAllSeriesAsync(
            Guid clubId,
            DateTime? date);
        Task<Model.Series> GetOneSeriesAsync(Guid guid);
        Task SaveNewSeries(Series series, Club club);
        Task SaveNewSeries(Series series);
        Task Update(Series model);
        Task Delete(Guid fleetId);
        Task UpdateSeriesResults(string clubInitials, string seasonName, string seriesName);
        Task UpdateSeriesResults(Guid seriesId);
    }
}