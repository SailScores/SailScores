using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;
using Entities = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public interface IClubService
    {
        Task<IList<Model.Club>> GetClubs(bool includeHidden);
        Task<Club> GetFullClub(string id);
        Task<Club> GetFullClub(Guid id);
        Task<Club> GetFullClubExceptScores(Guid id);
        Task<Club> GetFullClubExceptScores(string id);
        Task<Club> GetMinimalClub(Guid id);
        Task<Club> GetMinimalClub(string clubInitials);
        Task<Guid> SaveNewClub(Club club);
        Task SaveNewFleet(Fleet fleet);
        Task SaveNewSeason(Season season);
        Task<IList<Fleet>> GetAllFleets(Guid clubId);
        Task<IList<Fleet>> GetActiveFleets(Guid clubId);

        Task UpdateClub(Club club);
        Task<Guid> GetClubId(string initials);
        Task<Guid> CopyClubAsync(Guid copyFromClubId, Club targetClub);
        Task<IEnumerable<BoatClass>> GetAllBoatClasses(Guid clubId);
        Task<bool> DoesClubHaveCompetitors(Guid clubId);
        Task<IList<Fleet>> GetMinimalForSelectedBoatsFleets(Guid clubId);
        Task<IList<Entities.ClubSeasonStats>> GetClubStats(string clubInitials);
        Task<IList<Entities.SiteStats>> GetAllStats();
    }
}