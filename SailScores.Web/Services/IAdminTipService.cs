using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services
{
    public interface IAdminTipService
    {
        void AddTips(ref AdminViewModel viewModel);
    }
}