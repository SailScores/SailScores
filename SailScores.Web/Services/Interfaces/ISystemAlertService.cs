using SailScores.Core.Model;

namespace SailScores.Web.Services.Interfaces;

public interface ISystemAlertService
{
    Task<IEnumerable<SystemAlert>> GetActiveAlertsAsync();
}
