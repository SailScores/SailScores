using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IHandicapSystemService
{
    Task<IList<HandicapSystemSummary>> GetBaseSystemsAsync();
    Task<IList<HandicapSystemSummary>> GetClubSystemsAsync(Guid clubId);
    Task<Guid> CreateClubSystemAsync(CreateHandicapSystemViewModel model);
}
