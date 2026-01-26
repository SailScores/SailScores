using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IWebSiteAdminService
{
    Task<SiteAdminIndexViewModel> GetAllClubsAsync();
    Task<SiteAdminClubDetailsViewModel> GetClubDetailsAsync(string clubInitials);
    Task ResetClubInitialsCacheAsync();
    Task<(byte[] Data, string FileName)> BackupClubAsync(string clubInitials, string createdBy);
    Task ResetClubAsync(Guid clubId, ResetLevel resetLevel);
    Task RecalculateSeriesAsync(Guid seriesId, string updatedBy);
}
