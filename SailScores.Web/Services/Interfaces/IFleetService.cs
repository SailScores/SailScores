using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

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