using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface IFleetService
    {
        Task<Guid> SaveNew(Fleet fleet);
        Task Delete(Guid fleetId);
        Task Update(Fleet fleet);
        Task<Fleet> Get(Guid fleetId);
        Task<IEnumerable<Fleet>> GetAllFleetsForClub(Guid clubId);
        Task<IEnumerable<Series>> GetSeriesForFleet(Guid fleetId);
        Task<IEnumerable<DeletableInfo>> GetDeletableInfo(Guid clubId);
        Task<IDictionary<Guid, IEnumerable<Guid>>> GetClubRegattaFleets(Guid clubId);
        Task AddCompetitorToFleet(Guid fleetId, Guid competitorId);
        Task RemoveCompetitorFromFleet(Guid fleetId, Guid competitorId);
        Task<IDictionary<Guid, IList<Guid>>> GetCompetitorFleetMembership(Guid clubId);
    }
}