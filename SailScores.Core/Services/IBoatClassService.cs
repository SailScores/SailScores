using SailScores.Core.Model;
using System;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface IBoatClassService
    {
        Task SaveNew(BoatClass boatClass);
        Task Delete(Guid boatClassId);
        Task Update(BoatClass boatClass);
    }
}