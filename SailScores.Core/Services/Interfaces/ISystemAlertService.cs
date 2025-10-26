using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services;

public interface ISystemAlertService
{
    Task<IEnumerable<SystemAlert>> GetActiveAlertsAsync();
}
