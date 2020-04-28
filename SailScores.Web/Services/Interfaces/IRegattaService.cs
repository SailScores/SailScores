using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
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

    }
}