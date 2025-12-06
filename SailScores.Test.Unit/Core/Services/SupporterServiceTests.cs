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

public class SupporterServiceTests
{
    private readonly SupporterService _service;
    private readonly IMapper _mapper;
    private readonly ISailScoresContext _context;

    public SupporterServiceTests()
    {
        _context = Utilities.InMemoryContextBuilder.GetContext();
        _mapper = MapperBuilder.GetSailScoresMapper();

        _service = new SupporterService(_context, _mapper);
    }

    [Fact]
    public async Task GetVisibleSupportersAsync_ReturnsOnlyVisibleNonExpiredSupporters()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        var visibleSupporter = new Db.Supporter
        {
            Id = Guid.NewGuid(),
            Name = "Active Supporter",
            IsVisible = true,
            ExpirationDate = now.AddDays(30),
            CreatedDate = now
        };

        var expiredSupporter = new Db.Supporter
        {
            Id = Guid.NewGuid(),
            Name = "Expired Supporter",
            IsVisible = true,
            ExpirationDate = now.AddDays(-1),
            CreatedDate = now.AddDays(-60)
        };

        var hiddenSupporter = new Db.Supporter
        {
            Id = Guid.NewGuid(),
            Name = "Hidden Supporter",
            IsVisible = false,
            ExpirationDate = now.AddDays(30),
            CreatedDate = now
        };

        var noExpirationSupporter = new Db.Supporter
        {
            Id = Guid.NewGuid(),
            Name = "No Expiration Supporter",
            IsVisible = true,
            ExpirationDate = null,
            CreatedDate = now
        };

        _context.Supporters.Add(visibleSupporter);
        _context.Supporters.Add(expiredSupporter);
        _context.Supporters.Add(hiddenSupporter);
        _context.Supporters.Add(noExpirationSupporter);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetVisibleSupportersAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, s => s.Name == "Active Supporter");
        Assert.Contains(result, s => s.Name == "No Expiration Supporter");
    }

    [Fact]
    public async Task GetAllSupportersAsync_ReturnsAllSupporters()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        var supporter1 = new Db.Supporter
        {
            Id = Guid.NewGuid(),
            Name = "Supporter 1",
            IsVisible = true,
            CreatedDate = now
        };

        var supporter2 = new Db.Supporter
        {
            Id = Guid.NewGuid(),
            Name = "Supporter 2",
            IsVisible = false,
            CreatedDate = now
        };

        _context.Supporters.Add(supporter1);
        _context.Supporters.Add(supporter2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllSupportersAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetSupporterAsync_ReturnsSupporterById()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var supporterId = Guid.NewGuid();
        
        var supporter = new Db.Supporter
        {
            Id = supporterId,
            Name = "Test Supporter",
            IsVisible = true,
            CreatedDate = now
        };

        _context.Supporters.Add(supporter);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSupporterAsync(supporterId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Supporter", result.Name);
    }

    [Fact]
    public async Task DeleteSupporter_SetsIsVisibleToFalse()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var supporterId = Guid.NewGuid();
        
        var supporter = new Db.Supporter
        {
            Id = supporterId,
            Name = "Test Supporter",
            IsVisible = true,
            CreatedDate = now
        };

        _context.Supporters.Add(supporter);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteSupporter(supporterId);

        // Assert
        var deletedSupporter = _context.Supporters.First(s => s.Id == supporterId);
        Assert.False(deletedSupporter.IsVisible);
    }

    [Fact]
    public async Task GetVisibleSupportersAsync_ReturnsEmptyWhenNoVisibleSupporters()
    {
        // Act
        var result = await _service.GetVisibleSupportersAsync();

        // Assert
        Assert.Empty(result);
    }
}
