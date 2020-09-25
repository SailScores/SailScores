using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IRaceService
    {
        Task<RaceSummaryListViewModel> GetAllRaceSummariesAsync(
            string clubInitials,
            string seasonName,
            bool includeScheduled,
            bool includeAbandoned);
        Task<RaceViewModel> GetSingleRaceDetailsAsync(string clubInitials, Guid id);
        Task<RaceWithOptionsViewModel> GetBlankRaceWithOptions(
            string clubInitials,
            Guid? regattaId,
            Guid? seriesId);
        Task SaveAsync(RaceWithOptionsViewModel race);
        Task Delete(Guid id, string userName);
        Task AddOptionsToRace(RaceWithOptionsViewModel raceWithOptions);
        Task<Season> GetCurrentSeasonAsync(string clubInitials);
    }
}