using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IClubRequestService
{
    Task SubmitRequest(ClubRequestViewModel request);
    Task<IList<ClubRequestViewModel>> GetPendingRequests();
    Task<ClubRequestWithOptionsViewModel> GetRequest(Guid id);
    Task ProcessRequest(Guid id, bool test, Guid? copyFromClubId);
    Task UpdateRequest(ClubRequestViewModel vm);
    Task<bool> AreInitialsAllowed(string initials);
}