using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IRegattaService
    {
        Task<IEnumerable<RegattaSummary>> GetAllRegattaSummaryAsync(string clubInitials);
        Task<Regatta> GetRegattaAsync(string clubInitials, string season, string regattaName);
        Task SaveNew(RegattaWithOptionsViewModel model);
    }
}