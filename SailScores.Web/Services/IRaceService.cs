using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IRaceService
    {
        Task<IEnumerable<RaceSummaryViewModel>> GetAllRaceSummariesAsync(string clubInitials);
        Task<RaceViewModel> GetSingleRaceDetailsAsync(string clubInitials, Guid id);
    }
}