using AutoMapper;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Db = SailScores.Database.Entities;

namespace SailScores.Test.Unit.Core.Services;

public class SystemAlertServiceTests
{
    private readonly SystemAlertService _service;
    private readonly IMapper _mapper;
    private readonly ISailScoresContext _context;

    public SystemAlertServiceTests()
    {
        _context = Utilities.InMemoryContextBuilder.GetContext();
        _mapper = MapperBuilder.GetSailScoresMapper();

        _service = new SystemAlertService(_context, _mapper);
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ReturnsOnlyActiveAlerts()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        var activeAlert = new Db.SystemAlert
        {
            Id = Guid.NewGuid(),
            Content = "Test active alert",
            ExpiresUtc = now.AddDays(1),
            CreatedDate = now,
            IsDeleted = false
        };

        var expiredAlert = new Db.SystemAlert
        {
            Id = Guid.NewGuid(),
            Content = "Test expired alert",
            ExpiresUtc = now.AddDays(-1),
            CreatedDate = now.AddDays(-2),
            IsDeleted = false
        };

        var deletedAlert = new Db.SystemAlert
        {
            Id = Guid.NewGuid(),
            Content = "Test deleted alert",
            ExpiresUtc = now.AddDays(1),
            CreatedDate = now,
            IsDeleted = true
        };

        _context.SystemAlerts.Add(activeAlert);
        _context.SystemAlerts.Add(expiredAlert);
        _context.SystemAlerts.Add(deletedAlert);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveAlertsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(activeAlert.Content, result.First().Content);
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ReturnsEmptyWhenNoActiveAlerts()
    {
        // Act
        var result = await _service.GetActiveAlertsAsync();

        // Assert
        Assert.Empty(result);
    }
}
