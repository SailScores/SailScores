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
        Task SaveNewClub(Club club);
        Task SaveNewFleet(Fleet fleet);
        Task SaveNewSeason(Season season);
        Task<IList<Fleet>> GetAllFleets(Guid clubId);
        Task UpdateClub(Club clubObject);
        Task<Club> GetMinimalClub(Guid id);
        Task<Guid> GetClubId(string id);
    }
}