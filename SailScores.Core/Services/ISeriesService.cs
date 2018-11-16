using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface ISeriesService
    {
        Task<Core.Model.Series> GetSeriesDetailsAsync(
            string clubInitials,
            string seasonName,
            string seriesName);
        Task<IEnumerable<Model.Series>> GetAllSeriesAsync(Guid clubId);
        Task<Model.Series> GetOneSeriesAsync(Guid guid);
    }
}