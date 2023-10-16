using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IAdminService
{
    Task<AdminViewModel> GetClubForEdit(string clubInitials);
    Task<AdminViewModel> GetClub(string clubInitials);
    Task UpdateClub(Club clubObject);
    string GetLocaleShortName(string locale);
}