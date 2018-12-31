using Sailscores.Core.Model;
using Sailscores.Web.Models.Sailscores;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sailscores.Web.Services
{
    public interface IFleetService
    {
        Task<IList<FleetSummary>> GetAllFleetSummaryAsync(string clubInitials);
        Task<FleetSummary> GetFleetAsync(string clubInitials, string fleetName);
    }
}