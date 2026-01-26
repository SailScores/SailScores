using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IWebSiteAdminService
{
    Task<SiteAdminIndexViewModel> GetAllClubsAsync();
    Task<SiteAdminClubDetailsViewModel> GetClubDetailsAsync(string clubInitials);
    Task ResetClubInitialsCacheAsync();
    Task<string> BackupClubAsync(Guid clubId);
    Task ResetClubAsync(Guid clubId);
    Task RecalculateSeriesAsync(Guid seriesId, string updatedBy);
}
