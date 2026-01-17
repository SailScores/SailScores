using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IAdminService
{
    Task<AdminViewModel> GetClubForEdit(string clubInitials);
    Task<AdminViewModel> GetClub(string clubInitials);
    Task UpdateClub(Club clubObject);
    Task ProcessLogoFile(AdminEditViewModel model);
    Task<FileStreamResult> GetLogoAsync(Guid id);
    Task ResetClubAsync(Guid clubId, ResetLevel resetLevel);
}
