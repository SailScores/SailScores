using SailScores.Core.Model;
using Entities = SailScores.Database.Entities;
using SailScores.Web.Models.SailScores;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SailScores.Web.Services
{
    public interface IClubService
    {
        Task<Club> GetClubForClubHome(string clubInitials);
        Task<ClubStatsViewModel> GetClubStats(string clubInitials);
    }
}