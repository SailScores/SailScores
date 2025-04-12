using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Api.Dtos;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IRaceService
    {
        Task<IList<Model.Race>> GetRacesAsync(Guid clubId);
        Task<IList<Model.Race>> GetRecentRacesAsync(Guid clubId, int daysBack);
        Task<IList<Model.Race>> GetFullRacesAsync(
            Guid clubId,
            string seasonName,
            bool includeScheduled = true,
            bool includeAbandoned = true);
        Task<Race> GetRaceAsync(Guid raceId);
        Task<Guid> SaveAsync(RaceDto race);
        Task Delete(Guid raceId, string deletedBy);
        Task<int> GetRaceCountAsync(
            Guid clubId,
            DateTime? raceDate,
            Guid fleetId);
        Task<Season> GetMostRecentRaceSeasonAsync(Guid clubId);
        Task<bool> HasRacesAsync(Guid clubId);
        Task<IList<Guid>> GetStatsExcludedRaces(Guid clubId, Guid seasonId);
        Task<int> GetNewRaceNumberAsync(Guid clubId, Guid fleetId, DateTime? date, Guid? regattaId);
    }
}