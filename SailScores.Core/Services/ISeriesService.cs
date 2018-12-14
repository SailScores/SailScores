using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sailscores.Core.Model;

namespace Sailscores.Core.Services
{
    public interface ISeriesService
    {
        Task<Core.Model.Series> GetSeriesDetailsAsync(
            string clubInitials,
            string seasonName,
            string seriesName);
        Task<IEnumerable<Model.Series>> GetAllSeriesAsync(Guid clubId);
        Task<Model.Series> GetOneSeriesAsync(Guid guid);
        Task SaveNewSeries(Series ssSeries, Club club);
    }
}