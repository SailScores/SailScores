using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface ISupporterService
    {
        Task<IEnumerable<Supporter>> GetVisibleSupportersAsync();
        Task<IEnumerable<Supporter>> GetAllSupportersAsync();
        Task<Supporter> GetSupporterAsync(Guid id);
        Task SaveNewSupporter(Supporter supporter);
        Task UpdateSupporter(Supporter supporter);
        Task DeleteSupporter(Guid id);
    }
}
