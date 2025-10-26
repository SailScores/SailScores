using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Services;

public class SystemAlertService : ISystemAlertService
{
    private readonly ISailScoresContext _dbContext;
    private readonly IMapper _mapper;

    public SystemAlertService(
        ISailScoresContext dbContext,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SystemAlert>> GetActiveAlertsAsync()
    {
        var now = DateTime.UtcNow;
        var dbAlerts = await _dbContext.SystemAlerts
            .Where(a => !a.IsDeleted && a.ExpiresUtc > now)
            .OrderBy(a => a.CreatedDate)
            .ToListAsync();

        return _mapper.Map<IEnumerable<SystemAlert>>(dbAlerts);
    }
}
