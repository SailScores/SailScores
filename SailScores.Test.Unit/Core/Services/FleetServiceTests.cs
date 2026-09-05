using AutoMapper;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using Xunit;

namespace SailScores.Test.Unit.Core.Services;

public class FleetServiceTests
{
    FleetService _service;

    private readonly ISailScoresContext _context;
    private readonly Guid _clubId;
    private readonly IMapper _mapper;

    public FleetServiceTests()
    {
        _context = InMemoryContextBuilder.GetContext();
        _clubId = _context.Clubs.First().Id;
        _mapper = MapperBuilder.GetSailScoresMapper();
        _service = new FleetService(
            _context,
            _mapper);
    }

    [Fact]
    public async Task SaveNew_Always_SavesToDb()
    {
        var startingFleetCount = _context.Fleets.Count();

        var newFleet = new Fleet
        {
            Name = "myFleet",
            ShortName = "myFleet",
            NickName = "myFleet",
        };

        await _service.SaveNew(newFleet);

        Assert.NotEmpty(_context.Fleets
            .Where(f => f.Name == newFleet.Name));
        Assert.Equal(startingFleetCount + 1,
            _context.Fleets.Count());
    }

    [Fact]
    public async Task SaveNew_WithSpacesInShortName_SanitizesForUrl()
    {
        var startingFleetCount = _context.Fleets.Count();

        var newFleet = new Fleet
        {
            Name = "My Fleet With Spaces",
            ShortName = "My Fleet With Spaces",
            NickName = "myFleet",
            ClubId = _clubId
        };

        await _service.SaveNew(newFleet);

        var savedFleet = _context.Fleets
            .FirstOrDefault(f => f.Name == newFleet.Name);
        
        Assert.NotNull(savedFleet);
        Assert.Equal("my-fleet-with-spaces", savedFleet.ShortName);
        Assert.Equal(startingFleetCount + 1, _context.Fleets.Count());
    }

    [Fact]
    public async Task SaveNew_WithSpecialCharsInShortName_SanitizesForUrl()
    {
        var startingFleetCount = _context.Fleets.Count();

        var newFleet = new Fleet
        {
            Name = "My Fleet!@#$%",
            ShortName = "Fleet!@#$%",
            NickName = "myFleet",
            ClubId = _clubId
        };

        await _service.SaveNew(newFleet);

        var savedFleet = _context.Fleets
            .FirstOrDefault(f => f.Name == newFleet.Name);
        
        Assert.NotNull(savedFleet);
        Assert.Equal("fleet", savedFleet.ShortName);
        Assert.Equal(startingFleetCount + 1, _context.Fleets.Count());
    }

    [Fact]
    public async Task Delete_Fleet_RemovesFromDb()
    {
        // Arrange
        var boatClass = await _context.BoatClasses.FirstAsync(TestContext.Current.CancellationToken);
        var comp = await _context.Competitors.FirstAsync(TestContext.Current.CancellationToken);
        var newFleet = new Fleet
        {
            Name = "myFleet",
            ShortName = "myFleet",
            NickName ="myFleet",
            BoatClasses = new List<BoatClass>
            {
                new BoatClass
                {
                    Id = boatClass.Id,
                    ClubId = boatClass.ClubId,
                    Name = boatClass.Name
                }
            },
            Competitors = new List<Competitor>
            {
                new Competitor
                {
                    Id = comp.Id,
                    ClubId = comp.ClubId,
                    Name = comp.Name
                }
            }
        };

        await _service.SaveNew(newFleet);

        Assert.NotEmpty(_context.Fleets
            .Where(f => f.Name == newFleet.Name).SelectMany(
            f => f.FleetBoatClasses));

        var newFleetId = _context.Fleets
            .Where(f => f.Name == newFleet.Name).First().Id;

        //Act 
        await _service.Delete(newFleetId);

        // Assert
        Assert.Empty(_context.Fleets
            .Where(f => f.Name == newFleet.Name));

    }


    [Fact]
    public async Task Get_Fleet_ReturnsFromDb()
    {
        // Arrange
        var boatClass = await _context.BoatClasses.FirstAsync(TestContext.Current.CancellationToken);
        var newFleet = new Fleet
        {
            Name = "myFleet",
            ShortName = "myFleet",
            NickName = "myFleet",
            BoatClasses = new List<BoatClass>
            {
                new BoatClass
                {
                    Id = boatClass.Id,
                    ClubId = boatClass.ClubId,
                    Name = boatClass.Name
                }
            }

        };

        await _service.SaveNew(newFleet);

        Assert.NotEmpty(_context.Fleets
            .Where(f => f.Name == newFleet.Name).SelectMany(
            f => f.FleetBoatClasses));

        var newFleetId = _context.Fleets
            .Where(f => f.Name == newFleet.Name).First().Id;

        //Act 
        var testresult = await _service.Get(newFleetId);

        // Assert
        Assert.Equal(newFleet.Name, testresult.Name);

    }

    [Fact]
    public async Task GetAllFleetsForClub_ReturnFromDb()
    {
        // Arrange

        // Act
        var fleets = await _service.GetAllFleetsForClub(_clubId);

        // Assert
        Assert.NotEmpty(fleets);

    }

    [Fact]
    public async Task GetSeriesForFleet_returnsFromDb()
    {
        //Arrange
        var race = await _context.Races.FirstAsync(TestContext.Current.CancellationToken);
        var fleet = await _context.Fleets.FirstAsync(TestContext.Current.CancellationToken);
        var series = await _context.Series.FirstAsync(TestContext.Current.CancellationToken);

        race.Fleet = fleet;
        series.RaceSeries = new List<Database.Entities.SeriesRace>
        {
            new Database.Entities.SeriesRace
            {
                RaceId = race.Id,
                SeriesId = series.Id

            }
        };
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var returnedValue = await _service.GetSeriesForFleet(fleet.Id) ;

        // Assert
        Assert.NotEmpty(returnedValue);
    }

    [Fact]
    public async Task Update_WithSpacesInShortName_SanitizesForUrl()
    {
        // Arrange
        var newFleet = new Fleet
        {
            Name = "Original Fleet",
            ShortName = "originalfleet",
            NickName = "Original",
            ClubId = _clubId
        };

        await _service.SaveNew(newFleet);
        var fleetId = _context.Fleets.First(f => f.Name == newFleet.Name).Id;

        // Act
        var fleetToUpdate = await _service.Get(fleetId);
        fleetToUpdate.ShortName = "Updated Fleet Name";
        await _service.Update(fleetToUpdate);

        // Assert
        var updatedFleet = await _service.Get(fleetId);
        Assert.Equal("updated-fleet-name", updatedFleet.ShortName);
    }

    [Fact]
    public async Task GetDeletableInfo_FleetUsedInRace_ReturnsNotDeletable()
    {
        // Arrange
        var fleet = await _context.Fleets.FirstAsync(TestContext.Current.CancellationToken);
        var race = await _context.Races.FirstAsync(TestContext.Current.CancellationToken);
        race.Fleet = fleet;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = (await _service.GetDeletableInfo(_clubId)).ToList();
        var fleetInfo = result.First(f => f.Id == fleet.Id);

        // Assert
        Assert.False(fleetInfo.IsDeletable);
        Assert.Contains("races assigned", fleetInfo.Reason);
    }

    [Fact]
    public async Task GetDeletableInfo_FleetUsedInSeries_ReturnsNotDeletable()
    {
        // Arrange
        var fleet = await _context.Fleets.FirstAsync(TestContext.Current.CancellationToken);
        var series = await _context.Series.FirstAsync(TestContext.Current.CancellationToken);
        series.FleetId = fleet.Id;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = (await _service.GetDeletableInfo(_clubId)).ToList();
        var fleetInfo = result.First(f => f.Id == fleet.Id);

        // Assert
        Assert.False(fleetInfo.IsDeletable);
        Assert.Contains("filter", fleetInfo.Reason);
    }

    [Fact]
    public async Task GetDeletableInfo_FleetUsedInBothRaceAndSeries_ReturnsNotDeletableWithBothReasons()
    {
        // Arrange
        var fleet = await _context.Fleets.FirstAsync(TestContext.Current.CancellationToken);
        var race = await _context.Races.FirstAsync(TestContext.Current.CancellationToken);
        var series = await _context.Series.FirstAsync(TestContext.Current.CancellationToken);
        race.Fleet = fleet;
        series.FleetId = fleet.Id;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = (await _service.GetDeletableInfo(_clubId)).ToList();
        var fleetInfo = result.First(f => f.Id == fleet.Id);

        // Assert
        Assert.False(fleetInfo.IsDeletable);
        Assert.Contains("races assigned", fleetInfo.Reason);
        Assert.Contains("filter", fleetInfo.Reason);
    }

    [Fact]
    public async Task GetDeletableInfo_FleetNotUsed_ReturnsDeletable()
    {
        // Arrange
        var newFleet = new Fleet
        {
            Name = "Unused Fleet",
            ShortName = "unused",
            NickName = "Unused",
            ClubId = _clubId
        };
        await _service.SaveNew(newFleet);
        var fleetId = _context.Fleets.First(f => f.Name == newFleet.Name).Id;

        // Act
        var result = (await _service.GetDeletableInfo(_clubId)).ToList();
        var fleetInfo = result.First(f => f.Id == fleetId);

        // Assert
        Assert.True(fleetInfo.IsDeletable);
        Assert.Equal(string.Empty, fleetInfo.Reason);
    }

    [Fact]
    public async Task AddCompetitorToFleet_SelectedBoatsFleet_AddsCompetitorFleetRow()
    {
        // Arrange
        var fleet = await _context.Fleets.SingleAsync(
            f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats,
            TestContext.Current.CancellationToken);
        var newCompetitor = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "Not Yet In Fleet",
            ClubId = _clubId
        };
        _context.Competitors.Add(newCompetitor);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _service.AddCompetitorToFleet(fleet.Id, newCompetitor.Id);

        // Assert
        Assert.Contains(_context.CompetitorFleets,
            cf => cf.FleetId == fleet.Id && cf.CompetitorId == newCompetitor.Id);
    }

    [Fact]
    public async Task AddCompetitorToFleet_CompetitorAlreadyInFleet_IsIdempotent()
    {
        // Arrange
        var fleet = await _context.Fleets.SingleAsync(
            f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats,
            TestContext.Current.CancellationToken);
        var existingMember = await _context.CompetitorFleets.FirstAsync(
            cf => cf.FleetId == fleet.Id, TestContext.Current.CancellationToken);

        // Act / Assert (no throw)
        await _service.AddCompetitorToFleet(fleet.Id, existingMember.CompetitorId);

        Assert.Single(_context.CompetitorFleets
            .Where(cf => cf.FleetId == fleet.Id && cf.CompetitorId == existingMember.CompetitorId));
    }

    [Fact]
    public async Task AddCompetitorToFleet_FleetNotSelectedBoatsType_ThrowsInvalidOperationException()
    {
        // Arrange
        var fleet = await _context.Fleets.FirstAsync(
            f => f.FleetType == Api.Enumerations.FleetType.AllBoatsInClub && f.ClubId == _clubId,
            TestContext.Current.CancellationToken);
        var competitor = await _context.Competitors.FirstAsync(TestContext.Current.CancellationToken);

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddCompetitorToFleet(fleet.Id, competitor.Id));
    }

    [Fact]
    public async Task AddCompetitorToFleet_FleetDoesNotExist_ThrowsKeyNotFoundException()
    {
        var competitor = await _context.Competitors.FirstAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AddCompetitorToFleet(Guid.NewGuid(), competitor.Id));
    }

    [Fact]
    public async Task RemoveCompetitorFromFleet_SelectedBoatsFleet_RemovesCompetitorFleetRow()
    {
        // Arrange
        var fleet = await _context.Fleets.SingleAsync(
            f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats,
            TestContext.Current.CancellationToken);
        var existingMember = await _context.CompetitorFleets.FirstAsync(
            cf => cf.FleetId == fleet.Id, TestContext.Current.CancellationToken);

        // Act
        await _service.RemoveCompetitorFromFleet(fleet.Id, existingMember.CompetitorId);

        // Assert
        Assert.DoesNotContain(_context.CompetitorFleets,
            cf => cf.FleetId == fleet.Id && cf.CompetitorId == existingMember.CompetitorId);
    }

    [Fact]
    public async Task RemoveCompetitorFromFleet_CompetitorNotInFleet_IsIdempotent()
    {
        // Arrange
        var fleet = await _context.Fleets.SingleAsync(
            f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats,
            TestContext.Current.CancellationToken);
        var competitorNotInFleet = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "Not In Fleet",
            ClubId = _clubId
        };
        _context.Competitors.Add(competitorNotInFleet);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var memberCountBefore = await _context.CompetitorFleets.CountAsync(
            cf => cf.FleetId == fleet.Id,
            TestContext.Current.CancellationToken);

        // Act
        await _service.RemoveCompetitorFromFleet(fleet.Id, competitorNotInFleet.Id);

        // Assert
        var memberCountAfter = await _context.CompetitorFleets.CountAsync(
            cf => cf.FleetId == fleet.Id,
            TestContext.Current.CancellationToken);

        Assert.Equal(memberCountBefore, memberCountAfter);
        Assert.DoesNotContain(_context.CompetitorFleets,
            cf => cf.FleetId == fleet.Id && cf.CompetitorId == competitorNotInFleet.Id);
    }

    [Fact]
    public async Task GetCompetitorFleetMembership_Always_GroupsFleetIdsByCompetitorId()
    {
        // Arrange
        var fleet = await _context.Fleets.SingleAsync(
            f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats,
            TestContext.Current.CancellationToken);
        var existingMember = await _context.CompetitorFleets.FirstAsync(
            cf => cf.FleetId == fleet.Id, TestContext.Current.CancellationToken);

        // Act
        var membership = await _service.GetCompetitorFleetMembership(_clubId);

        // Assert
        Assert.True(membership.ContainsKey(existingMember.CompetitorId));
        Assert.Contains(fleet.Id, membership[existingMember.CompetitorId]);
    }

    [Fact]
    public async Task GetCompetitorFleetMembership_CompetitorInNoFleets_OmittedFromResult()
    {
        // Arrange
        var newCompetitor = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "No Fleets",
            ClubId = _clubId
        };
        _context.Competitors.Add(newCompetitor);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var membership = await _service.GetCompetitorFleetMembership(_clubId);

        // Assert
        Assert.False(membership.ContainsKey(newCompetitor.Id));
    }
}
