using SailScores.Core.Model;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class SystemAlertService : ISystemAlertService
{
    private readonly Core.Services.ISystemAlertService _coreSystemAlertService;

    public SystemAlertService(Core.Services.ISystemAlertService coreSystemAlertService)
    {
        _coreSystemAlertService = coreSystemAlertService;
    }

    public async Task<IEnumerable<SystemAlert>> GetActiveAlertsAsync()
    {
        return await _coreSystemAlertService.GetActiveAlertsAsync();
    }
}
