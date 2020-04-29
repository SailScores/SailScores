using SailScores.Core.Model;
using System;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface IFleetService
    {
        Task<Guid> SaveNew(Fleet fleet);
        Task Delete(Guid fleetId);
        Task Update(Fleet fleet);
        Task<Fleet> Get(Guid fleetId);
    }
}