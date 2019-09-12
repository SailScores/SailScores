using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IRaceService
    {
        Task<IEnumerable<RaceSummaryViewModel>> GetAllRaceSummariesAsync(
            string clubInitials,
            bool includeScheduled,
            bool includeAbandoned);
        Task<RaceViewModel> GetSingleRaceDetailsAsync(string clubInitials, Guid id);
        Task<RaceWithOptionsViewModel> GetBlankRaceWithOptions(
            string clubInitials,
            Guid? regattaId,
            Guid? seriesId);
        Task SaveAsync(RaceWithOptionsViewModel race);
        Task Delete(Guid id);
        Task AddOptionsToRace(RaceWithOptionsViewModel raceWithOptions);
    }
}