using AutoMapper;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services;

public class HandicapServiceTests
{
    private readonly ISailScoresContext _context;
    private readonly IMapper _mapper;
    private readonly HandicapService _service;
    private readonly Guid _clubId;

    public HandicapServiceTests()
    {
        _context = InMemoryContextBuilder.GetContext();
        _mapper = MapperBuilder.GetSailScoresMapper();
        _service = new HandicapService(_context, _mapper);
        _clubId = _context.Clubs.First().Id;
    }

    [Fact]
    public async Task GetBaseHandicapSystemsAsync_ReturnsOnlySiteWideSystems()
    {
        var baseSystem = new SailScores.Database.Entities.HandicapSystem
        {
            Id = Guid.NewGuid(),
            ClubId = null,
            Name = "Base Portsmouth",
            SystemType = SailScores.Database.Entities.HandicapSystemType.Portsmouth
        };
        var clubSystem = new SailScores.Database.Entities.HandicapSystem
        {
            Id = Guid.NewGuid(),
            ClubId = _clubId,
            Name = "Club Portsmouth",
            SystemType = SailScores.Database.Entities.HandicapSystemType.Portsmouth
        };
        _context.HandicapSystems.Add(baseSystem);
        _context.HandicapSystems.Add(clubSystem);
        await _context.SaveChangesAsync();

        var result = await _service.GetBaseHandicapSystemsAsync();

        Assert.Contains(result, s => s.Id == baseSystem.Id);
        Assert.DoesNotContain(result, s => s.Id == clubSystem.Id);
    }

    [Fact]
    public async Task GetHandicapSystemsAsync_ReturnsOnlyClubSystems()
    {
        var baseSystem = new SailScores.Database.Entities.HandicapSystem
        {
            Id = Guid.NewGuid(),
            ClubId = null,
            Name = "Base ToD",
            SystemType = SailScores.Database.Entities.HandicapSystemType.PhrfToD
        };
        var clubSystem = new SailScores.Database.Entities.HandicapSystem
        {
            Id = Guid.NewGuid(),
            ClubId = _clubId,
            Name = "Club ToD",
            SystemType = SailScores.Database.Entities.HandicapSystemType.PhrfToD
        };
        _context.HandicapSystems.Add(baseSystem);
        _context.HandicapSystems.Add(clubSystem);
        await _context.SaveChangesAsync();

        var result = await _service.GetHandicapSystemsAsync(_clubId);

        Assert.Contains(result, s => s.Id == clubSystem.Id);
        Assert.DoesNotContain(result, s => s.Id == baseSystem.Id);
    }

    [Fact]
    public async Task CreateClubHandicapSystemAsync_UsesParentAndInheritsType()
    {
        var baseSystem = new SailScores.Database.Entities.HandicapSystem
        {
            Id = Guid.NewGuid(),
            ClubId = null,
            Name = "Base PHRF ToT",
            SystemType = SailScores.Database.Entities.HandicapSystemType.PhrfToT
        };
        _context.HandicapSystems.Add(baseSystem);
        await _context.SaveChangesAsync();

        var created = await _service.CreateClubHandicapSystemAsync(
            _clubId,
            baseSystem.Id,
            "Club Inherited ToT",
            "Test description");

        Assert.Equal(_clubId, created.ClubId);
        Assert.Equal(baseSystem.Id, created.ParentSystemId);
        Assert.Equal(HandicapSystemType.PhrfToT, created.SystemType);

        var inDb = _context.HandicapSystems.Single(h => h.Id == created.Id);
        Assert.Equal(baseSystem.Id, inDb.ParentSystemId);
    }

    [Fact]
    public async Task CreateClubHandicapSystemAsync_WithNonBaseParent_Throws()
    {
        var nonBaseSystem = new SailScores.Database.Entities.HandicapSystem
        {
            Id = Guid.NewGuid(),
            ClubId = _clubId,
            Name = "Existing Club System",
            SystemType = SailScores.Database.Entities.HandicapSystemType.Portsmouth
        };
        _context.HandicapSystems.Add(nonBaseSystem);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateClubHandicapSystemAsync(
                _clubId,
                nonBaseSystem.Id,
                "Invalid Child",
                "Should fail"));
    }
}
