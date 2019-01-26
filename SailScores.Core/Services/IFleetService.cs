using SailScores.Core.Model;
using System;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface IFleetService
    {
        Task SaveNew(Fleet fleet);
        Task Delete(Guid fleetId);
        Task Update(Fleet fleet);
    }
}