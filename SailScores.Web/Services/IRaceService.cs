using Sailscores.Core.Model;
using Sailscores.Web.Models.Sailscores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sailscores.Web.Services
{
    public interface IRaceService
    {
        Task<IEnumerable<RaceSummaryViewModel>> GetAllRaceSummariesAsync(string clubInitials);
        Task<RaceViewModel> GetSingleRaceDetailsAsync(string clubInitials, Guid id);
    }
}