using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IRegattaService
    {
        Task<IList<Model.Regatta>> GetAllRegattasAsync(
            Guid clubId);
        Task<IList<Model.Regatta>> GetRegattasDuringSpanAsync(
            DateTime start,
            DateTime end);
        Task<Core.Model.Regatta> GetRegattaAsync(
            string clubInitials,
            string seasonName,
            string regattaName);
        Task SaveNewRegattaAsync(Regatta regatta);
        Task UpdateAsync(Regatta model);
        Task AddRaceToRegattaAsync(Race race, Guid regattaId);
        Task DeleteAsync(Guid regattaId);
    }
}