using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace SailScores.Web.Services
{
    public interface IClubService
    {
        Task<Club> GetClubForClubHome(string clubInitials);
        Task<ClubStatsViewModel> GetClubStats(string clubInitials);
        Task<IEnumerable<AllClubStatsViewModel>> GetAllClubStats();
        Task UpdateStatsDescription(string clubInitials, string statisticsDescription);

        Task<Guid> GetClubId(string initials);
    }
}