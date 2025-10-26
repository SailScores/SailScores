using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface ISystemAlertService
{
    Task<IEnumerable<SystemAlertViewModel>> GetActiveAlertsAsync();
}
