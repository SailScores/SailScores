using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IRaceService
    {
        Task<IEnumerable<Race>> GetAllRaceSummariesAsync(string clubInitials);
        Task<Race> GetSingleRaceDetailsAsync(string clubInitials, Guid id);
    }
}