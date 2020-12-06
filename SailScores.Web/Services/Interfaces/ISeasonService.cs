using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Web.Services
{
    public interface ISeasonService
    {
        Task SaveNew(Season season);
        Task Update(Season season);
        Task Delete(Guid seasonId);
        Task<IList<Season>> GetSeasons(Guid clubId);
        
        Task<IList<String>> GetSavingSeasonErrors(Season model);

        Task<Season> GetSeasonSuggestion(Guid clubId);
    }
}