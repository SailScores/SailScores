using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Api.Dtos;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IClubService
    {
        Task<IList<Model.Club>> GetClubs(bool includeHidden);
        Task<Club> GetFullClub(string id);
        Task<Club> GetFullClub(Guid id);
        Task<Guid> SaveNewClub(Club club);
        Task SaveNewFleet(Fleet fleet);
        Task SaveNewSeason(Season season);
        Task<IList<Fleet>> GetAllFleets(Guid clubId);
        Task UpdateClub(Club clubObject);
        Task<Club> GetMinimalClub(Guid id);
        Task<Club> GetMinimalClub(string clubInitials);
        Task<Guid> GetClubId(string initials);
        Task<Guid> CopyClubAsync(Guid copyFromClubId, Club targetClub);
        Task<IEnumerable<BoatClass>> GetAllBoatClasses(Guid clubId);
        Task<bool> DoesClubHaveCompetitors(Guid clubId);
        Task<IList<Fleet>> GetMinimalForSelectedBoatsFleets(Guid clubId);
    }
}