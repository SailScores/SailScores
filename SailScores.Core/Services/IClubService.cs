using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IClubService
    {
        Task<IList<Model.Club>> GetClubs(bool includeHidden);
        Task<Club> GetFullClub(string id);
        Task<Club> GetFullClub(Guid id);
        Task SaveNewClub(Club club);
        Task SaveNewBoatClass(BoatClass boatClass);
        Task SaveNewFleet(Fleet fleet);
        Task SaveNewSeason(Season season);
    }
}