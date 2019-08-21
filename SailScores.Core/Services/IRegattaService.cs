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
        Task<Core.Model.Regatta> GetRegattaAsync(
            string clubInitials,
            string seasonName,
            string regattaName);
        Task SaveNewRegatta(Regatta regatta, Club club);
        Task SaveNewRegatta(Regatta regatta);
        Task Update(Regatta model);
        Task Delete(Guid regattaId);
    }
}