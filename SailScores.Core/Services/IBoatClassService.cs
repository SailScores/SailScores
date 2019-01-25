using SailScores.Core.Model;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface IBoatClassService
    {
        Task SaveNew(BoatClass boatClass);
        Task Delete(BoatClass boatClass);
        Task Update(BoatClass boatClass);
    }
}