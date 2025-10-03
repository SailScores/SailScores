using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

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
        Guid? seriesId,
        Guid? fleetId = null);
    Task SaveAsync(RaceWithOptionsViewModel race);
    Task Delete(Guid id, string userName);
    Task AddOptionsToRace(RaceWithOptionsViewModel raceWithOptions);
    Task<Season> GetCurrentSeasonAsync(string clubInitials);
    Task<RaceWithOptionsViewModel> FixupRaceWithOptions(
        string clubInitials,
        RaceWithOptionsViewModel race);
}