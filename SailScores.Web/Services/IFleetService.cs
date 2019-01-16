using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IFleetService
    {
        Task<IList<FleetSummary>> GetAllFleetSummaryAsync(string clubInitials);
        Task<FleetSummary> GetFleetAsync(string clubInitials, string fleetName);
    }
}