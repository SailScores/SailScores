using SailScores.Web.Models.SailScores;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface ISeriesService
    {
        Task<IEnumerable<SeriesSummary>> GetAllSeriesSummaryAsync(string clubInitials);
        Task<SeriesSummary> GetSeriesAsync(string clubInitials, string season, string seriesName);
    }
}