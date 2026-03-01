using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SailScores.Core.Mapping;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services;

/// <summary>
/// Tests for Series Fleet Option feature - Fleet-based competitor filtering.
/// 
/// These tests verify that when a series has a FleetId assigned, the series
/// correctly persists and loads the FleetId and UseFullRaceScores properties.
/// 
/// Phase 3 implementation will add:
/// - PopulateCompetitorsAsync filtering by fleet
/// - BaseScoringCalculator position recalculation
/// </summary>
public class SeriesFleetOptionTests
{
    private readonly ISailScoresContext _context;
    private readonly IMapper _mapper;
    private readonly SeriesService _seriesService;
    private readonly Mock<IScoringCalculatorFactory> _mockScoringCalculatorFactory;
    private readonly DbObjectBuilder _dbObjectBuilder;
    private readonly IMemoryCache _realCache;

    public SeriesFleetOptionTests()
    {
        _context = InMemoryContextBuilder.GetContext();

        var config = new MapperConfiguration(opts =>
        {
            opts.AddProfile(new DbToModelMappingProfile());
        });
        _mapper = config.CreateMapper();

        // Setup mock scoring calculator
        var mockCalculator = new Mock<IScoringCalculator>();
        mockCalculator.Setup(c => c.CalculateResults(It.IsAny<Series>())).Returns(new SeriesResults
        {
            Results = new Dictionary<Competitor, SeriesCompetitorResults>()
        });

        _mockScoringCalculatorFactory = new Mock<IScoringCalculatorFactory>();
        _mockScoringCalculatorFactory.Setup(f => f.CreateScoringCalculatorAsync(It.IsAny<ScoringSystem>()))
            .ReturnsAsync(mockCalculator.Object);

        _dbObjectBuilder = new DbObjectBuilder(_context, _mapper);
        _realCache = new MemoryCache(new MemoryCacheOptions());

        _seriesService = new SeriesService(
            _mockScoringCalculatorFactory.Object,
            new Mock<IScoringService>().Object,
            new Mock<IForwarderService>().Object,
            new Mock<IConversionService>().Object,
            _dbObjectBuilder,
            _context,
            _realCache,
            _mapper,
            new Mock<IIndexNowService>().Object
        );
    }

    [Fact]
    public async Task SaveNewSeries_WithFleetId_PersistsFleetId()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();
        var fleet = _context.Fleets.First(f => f.ClubId == clubId);

        var series = new Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Fleet-Filtered Series",
            Season = _mapper.Map<Season>(season),
            Type = SeriesType.Standard,
            FleetId = fleet.Id,
            UseFullRaceScores = null,
            UpdatedBy = "test"
        };

        // Act
        var seriesId = await _seriesService.SaveNewSeries(series);

        // Assert - FleetId should be persisted
        var dbSeries = await _context.Series.SingleAsync(s => s.Id == seriesId);
        Assert.Equal(fleet.Id, dbSeries.FleetId);
    }

    [Fact]
    public async Task SaveNewSeries_WithUseFullRaceScoresTrue_PersistsValue()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();
        var fleet = _context.Fleets.First(f => f.ClubId == clubId);

        var series = new Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Full Race Scores Series",
            Season = _mapper.Map<Season>(season),
            Type = SeriesType.Standard,
            FleetId = fleet.Id,
            UseFullRaceScores = true, // Use full race positions
            UpdatedBy = "test"
        };

        // Act
        var seriesId = await _seriesService.SaveNewSeries(series);

        // Assert - UseFullRaceScores should be persisted as true
        var dbSeries = await _context.Series.SingleAsync(s => s.Id == seriesId);
        Assert.True(dbSeries.UseFullRaceScores);
    }

    [Fact]
    public async Task SaveNewSeries_WithUseFullRaceScoresFalse_PersistsValue()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();
        var fleet = _context.Fleets.First(f => f.ClubId == clubId);

        var series = new Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Recalculated Positions Series",
            Season = _mapper.Map<Season>(season),
            Type = SeriesType.Standard,
            FleetId = fleet.Id,
            UseFullRaceScores = false, // Recalculate positions by fleet
            UpdatedBy = "test"
        };

        // Act
        var seriesId = await _seriesService.SaveNewSeries(series);

        // Assert - UseFullRaceScores should be persisted as false
        var dbSeries = await _context.Series.SingleAsync(s => s.Id == seriesId);
        Assert.False(dbSeries.UseFullRaceScores == true);
    }

    [Fact]
    public async Task SaveNewSeries_WithoutFleetId_PersistsAsNull()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();

        var series = new Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Open Series",
            Season = _mapper.Map<Season>(season),
            Type = SeriesType.Standard,
            FleetId = null, // No fleet filter
            UseFullRaceScores = null,
            UpdatedBy = "test"
        };

        // Act
        var seriesId = await _seriesService.SaveNewSeries(series);

        // Assert - FleetId should remain null
        var dbSeries = await _context.Series.SingleAsync(s => s.Id == seriesId);
        Assert.Null(dbSeries.FleetId);
    }

    [Fact]
    public async Task GetSeriesDetailsAsync_WithFleetAssigned_LoadsFleetId()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var clubInitials = _context.Clubs.First().Initials;
        var season = _context.Seasons.First();
        var fleet = _context.Fleets.First(f => f.ClubId == clubId);

        var dbSeries = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Test Series",
            UrlName = "test-series",
            Season = season,
            Type = Database.Entities.SeriesType.Standard,
            FleetId = fleet.Id,
            UseFullRaceScores = false,
            RaceSeries = new List<Database.Entities.SeriesRace>()
        };
        _context.Series.Add(dbSeries);
        await _context.SaveChangesAsync();

        // Act
        var series = await _seriesService.GetSeriesDetailsAsync(clubInitials, season.UrlName, "test-series");

        // Assert - FleetId should be loaded
        Assert.NotNull(series);
        Assert.Equal(fleet.Id, series.FleetId);
        Assert.False(series.UseFullRaceScores == true);
    }

    [Fact]
    public async Task UpdateSeries_ModifyFleetId_PersistsChanges()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();
        var fleet1 = _context.Fleets.First(f => f.ClubId == clubId);

        // Create another fleet
        var fleet2 = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Second Fleet",
            ClubId = clubId,
            IsActive = true,
            FleetType = Api.Enumerations.FleetType.SelectedBoats
        };
        _context.Fleets.Add(fleet2);
        await _context.SaveChangesAsync();

        // Create series with first fleet
        var series = new Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Series to Update",
            Season = _mapper.Map<Season>(season),
            Type = SeriesType.Standard,
            FleetId = fleet1.Id,
            UseFullRaceScores = false,
            UpdatedBy = "test"
        };
        var seriesId = await _seriesService.SaveNewSeries(series);

        // Act - Update series to use second fleet
        var seriesToUpdate = _mapper.Map<Series>(await _context.Series.SingleAsync(s => s.Id == seriesId));
        seriesToUpdate.FleetId = fleet2.Id;
        seriesToUpdate.UseFullRaceScores = true;
        await _seriesService.Update(seriesToUpdate);

        // Assert - Changes should be persisted
        var dbUpdated = await _context.Series.SingleAsync(s => s.Id == seriesId);
        Assert.Equal(fleet2.Id, dbUpdated.FleetId);
        Assert.True(dbUpdated.UseFullRaceScores);
    }

    [Fact]
    public async Task RemoveFleetFromSeries_SetFleetIdToNull_PersistsChange()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();
        var fleet = _context.Fleets.First(f => f.ClubId == clubId);

        var series = new Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Series With Fleet",
            Season = _mapper.Map<Season>(season),
            Type = SeriesType.Standard,
            FleetId = fleet.Id,
            UseFullRaceScores = false,
            UpdatedBy = "test"
        };
        var seriesId = await _seriesService.SaveNewSeries(series);

        // Act - Remove fleet filter
        var seriesToUpdate = _mapper.Map<Series>(await _context.Series.SingleAsync(s => s.Id == seriesId));
        seriesToUpdate.FleetId = null;
        seriesToUpdate.UseFullRaceScores = null;
        await _seriesService.Update(seriesToUpdate);

        // Assert - FleetId should be null
        var dbUpdated = await _context.Series.SingleAsync(s => s.Id == seriesId);
        Assert.Null(dbUpdated.FleetId);
    }

    [Fact]
    public async Task SaveNewSeries_WithInactiveFleet_PersistsFleetId()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();

        // Create an inactive fleet
        var inactiveFleet = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Fleet",
            ClubId = clubId,
            IsActive = false,
            FleetType = Api.Enumerations.FleetType.SelectedBoats
        };
        _context.Fleets.Add(inactiveFleet);
        await _context.SaveChangesAsync();

        var series = new Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Inactive Fleet Series",
            Season = _mapper.Map<Season>(season),
            Type = SeriesType.Standard,
            FleetId = inactiveFleet.Id,
            UseFullRaceScores = null,
            UpdatedBy = "test"
        };

        // Act
        var seriesId = await _seriesService.SaveNewSeries(series);

        // Assert - Should persist inactive fleet reference
        var dbSeries = await _context.Series.SingleAsync(s => s.Id == seriesId);
        Assert.Equal(inactiveFleet.Id, dbSeries.FleetId);
    }

    [Fact]
    public async Task RoundTrip_SeriesWithFleetAndUseFullRaceScores_ValuesPreserved()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var clubInitials = _context.Clubs.First().Initials;
        var season = _context.Seasons.First();
        var fleet = _context.Fleets.First(f => f.ClubId == clubId);

        var originalSeries = new Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Round Trip Test",
            Season = _mapper.Map<Season>(season),
            Type = SeriesType.Standard,
            FleetId = fleet.Id,
            UseFullRaceScores = true,
            UpdatedBy = "test"
        };

        // Act - Save and reload
        var seriesId = await _seriesService.SaveNewSeries(originalSeries);
        var loaded = await _seriesService.GetSeriesDetailsAsync(clubInitials, season.UrlName, "round-trip-test");

        // Assert - All values should match
        Assert.NotNull(loaded);
        Assert.Equal(fleet.Id, loaded.FleetId);
        Assert.True(loaded.UseFullRaceScores);
    }
}
