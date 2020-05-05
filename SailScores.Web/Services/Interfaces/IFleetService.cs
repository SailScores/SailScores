using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IFleetService
    {
        Task<IList<FleetSummary>> GetAllFleetSummary(string clubInitials);
        Task<FleetSummary> GetFleet(string clubInitials, string fleetShortName);
        Task<Fleet> GetFleet(Guid fleetId);
        Task SaveNew(FleetWithOptionsViewModel fleet);
        Task Delete(Guid fleetId);
        Task Update(FleetWithOptionsViewModel fleet);
        Task<FleetWithOptionsViewModel> GetBlankFleetWithOptionsAsync(
            string clubInitials,
            Guid? regattaId);
    }
}