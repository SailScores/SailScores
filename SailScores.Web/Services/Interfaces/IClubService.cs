using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IClubService
{
    Task<Club> GetClubForClubHome(string clubInitials);
    Task<ClubStatsViewModel> GetClubStats(string clubInitials);
    Task<IEnumerable<AllClubStatsViewModel>> GetAllClubStats();
    Task UpdateStatsDescription(string clubInitials, string statisticsDescription);

    Task<Guid> GetClubId(string initials);

    Task SetUseAdvancedFeaturesAsync(Guid clubId, bool enabled);
    Task SetSubscriptionTypeAsync(Guid clubId, string subscriptionType);
    Task<Club> GetClubByIdAsync(Guid clubId);
}
