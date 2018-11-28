using Sailscores.Web.Models.Sailscores;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sailscores.Web.Services
{
    public interface ISeriesService
    {
        Task<IEnumerable<SeriesSummary>> GetAllSeriesSummaryAsync(string clubInitials);
        Task<Core.Model.Series> GetSeriesAsync(string clubInitials, string season, string seriesName);
    }
}