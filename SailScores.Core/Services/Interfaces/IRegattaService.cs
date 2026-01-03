using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IRegattaService
    {
        Task<IList<Model.Regatta>> GetAllRegattasAsync(
            Guid clubId);
        Task<IList<Model.Regatta>> GetRegattasDuringSpanAsync(
            DateTime start,
            DateTime end);
        Task<Regatta> GetRegattaAsync(Guid regattaId);
        Task<Core.Model.Regatta> GetRegattaAsync(
            string clubInitials,
            string seasonName,
            string regattaName);
        Task<Guid> SaveNewRegattaAsync(Regatta regatta);
        Task<Guid> UpdateAsync(Regatta model);
        Task AddRaceToRegattaAsync(Race race, Guid regattaId);
        Task DeleteAsync(Guid regattaId);
        Task AddFleetToRegattaAsync(Guid fleetId, Guid regattaId);
        Task<Regatta> GetRegattaForRace(Guid raceId);
        Task<Regatta> GetRegattaForFleet(Guid fleetId);
        Task<bool> ClubHasRegattasAsync(Guid clubId);
    }
}