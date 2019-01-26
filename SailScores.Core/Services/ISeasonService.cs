using SailScores.Core.Model;
using System;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface ISeasonService
    {
        Task SaveNew(Season season);
        Task Delete(Guid seasonId);
        Task Update(Season season);
    }
}