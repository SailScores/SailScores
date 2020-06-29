using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IAdminService
    {
        Task<AdminViewModel> GetClubForEdit(string clubInitials);
        Task<AdminViewModel> GetClub(string clubInitials);
        Task UpdateClub(Club clubObject);
    }
}