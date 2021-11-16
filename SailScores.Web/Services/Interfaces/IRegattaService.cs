using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IRegattaService
{
    Task<IEnumerable<RegattaSummaryViewModel>> GetAllRegattaSummaryAsync(string clubInitials);
    Task<Regatta> GetRegattaAsync(Guid regattaId);
    Task<Regatta> GetRegattaAsync(string clubInitials, string season, string regattaName);
    Task<Guid> SaveNewAsync(RegattaWithOptionsViewModel model);
    Task<Guid> UpdateAsync(RegattaWithOptionsViewModel model);
    Task DeleteAsync(Guid regattaId);

    Task<IEnumerable<RegattaSummaryViewModel>> GetCurrentRegattas();
    Task AddFleetToRegattaAsync(Guid fleetId, Guid regattaId);
    Task<RegattaWithOptionsViewModel> GetBlankRegattaWithOptions(Guid clubId);
}