using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IBoatClassService
{
    Task SaveNew(BoatClass boatClass);
    Task Delete(Guid boatClassId);
    Task Update(BoatClass boatClass);
    Task<BoatClass> GetClass(Guid boatClassId);
    Task<BoatClassDeleteViewModel> GetClassDeleteViewModel(Guid id);
}