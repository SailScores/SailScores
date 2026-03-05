using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SailScores.Core.Mapping;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services;

/// <summary>
/// Tests SeriesService fleet filtering with a real Appendix A scoring calculator
/// to ensure accurate score recalculation when series has a FleetId filter.
/// </summary>
public class SeriesServiceFleetFilteringTests
{
    private readonly IMapper _mapper;
    private readonly ISailScoresContext _context;
    private readonly SeriesService _service;
    private readonly IScoringCalculatorFactory _scoringCalculatorFactory;
    private readonly IMemoryCache _cache;
    private readonly Guid _clubId;
    private readonly Guid _boatClassId;
    private readonly Database.Entities.Season _season;

    public SeriesServiceFleetFilteringTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _context = Utilities.InMemoryContextBuilder.GetContext();
        _clubId = _context.Clubs.First().Id;
        _boatClassId = _context.BoatClasses.First().Id;
        _season = _context.Seasons.First();

        var config = new MapperConfiguration(opts =>
        {
            opts.AddProfile(new DbToModelMappingProfile());
        });
        _mapper = config.CreateMapper();

        // Use real scoring calculator factory and service (not mocked)
        _scoringCalculatorFactory = new ScoringCalculatorFactory(_context, _cache);
        var scoringService = new ScoringService(_context, _cache, _mapper);

        var dbObjectBuilder = new DbObjectBuilder(_context, _mapper);
        var mockForwarderService = new Moq.Mock<IForwarderService>();
        var mockConversionService = new Moq.Mock<IConversionService>();
        var mockIndexNowService = new Moq.Mock<IIndexNowService>();

        _service = new SeriesService(
            _scoringCalculatorFactory,
            scoringService,
            mockForwarderService.Object,
            mockConversionService.Object,
            dbObjectBuilder,
            _context,
            _cache,
            _mapper,
            mockIndexNowService.Object
        );
    }

    private Database.Entities.ScoringSystem CreateAppendixAScoringSystem()
    {
        var system = new Database.Entities.ScoringSystem
        {
            Id = Guid.NewGuid(),
            Name = "Appendix A Low Point",
            ClubId = _clubId,
            DiscardPattern = "0,1",
            ParentSystemId = null,
            ScoreCodes = new List<Database.Entities.ScoreCode>()
        };

        // Create score codes after we have the system ID
        var scoreCodes = new List<Database.Entities.ScoreCode>
        {
            new Database.Entities.ScoreCode
            {
                Id = Guid.NewGuid(),
                ScoringSystemId = system.Id,
                Name = "DNC",
                Description = "Did not come to the starting area",
                PreserveResult = false,
                Discardable = true,
                Started = false,
                FormulaValue = 1,
                AdjustOtherScores = null,
                CameToStart = false,
                Finished = false,
                Formula = "SER+"
            },
            new Database.Entities.ScoreCode
            {
                Id = Guid.NewGuid(),
                ScoringSystemId = system.Id,
                Name = "DNF",
                Description = "Started but did not finish",
                PreserveResult = false,
                Discardable = true,
                Started = true,
                FormulaValue = 1,
                AdjustOtherScores = null,
                CameToStart = true,
                Finished = false,
                Formula = "SER+"
            },
            new Database.Entities.ScoreCode
            {
                Id = Guid.NewGuid(),
                ScoringSystemId = system.Id,
                Name = "OCS",
                Description = "On course side at start or broke rule 30.1",
                PreserveResult = false,
                Discardable = true,
                Started = false,
                FormulaValue = 1,
                AdjustOtherScores = null,
                CameToStart = true,
                Finished = false,
                Formula = "SER+"
            },
            new Database.Entities.ScoreCode
            {
                Id = Guid.NewGuid(),
                ScoringSystemId = system.Id,
                Name = "RET",
                Description = "Retired",
                PreserveResult = true,
                Discardable = true,
                Started = true,
                FormulaValue = 1,
                AdjustOtherScores = true,
                CameToStart = true,
                Finished = true,
                Formula = "CTS+"
            }
        };

        system.ScoreCodes = scoreCodes;

        _context.ScoringSystems.Add(system);
        _context.SaveChanges();
        return system;
    }

    [Fact]
    public async Task ScoringSystemLookup_CanFindScoringSystem()
    {
        // Test that scoring system lookup works correctly
        var scoringSystem = CreateAppendixAScoringSystem();

        // Try to look it up directly
        var loaded = await _context.ScoringSystems.FirstAsync(s => s.Id == scoringSystem.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Appendix A Low Point", loaded.Name);
    }

    [Fact]
    public async Task FleetFiltering_TwoFleetsTwoRaces_OnlyIncludesTargetFleetCompetitors()
    {
        // Arrange - Create two fleets with different competitors
        var fleetA = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        var fleetB = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet B",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        _context.Fleets.Add(fleetA);
        _context.Fleets.Add(fleetB);

        // Create competitors - 2 in Fleet A, 2 in Fleet B
        var compA1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A Comp 1",
            BoatName = "A1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compA2 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A Comp 2",
            BoatName = "A2",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compB1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "Fleet B Comp 1",
            BoatName = "B1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetB.Id }
            }
        };
        var compB2 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "Fleet B Comp 2",
            BoatName = "B2",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetB.Id }
            }
        };

        _context.Competitors.AddRange(compA1, compA2, compB1, compB2);

        // Create two races - Race 1 in Fleet A, Race 2 in Fleet B
        var race1 = new Database.Entities.Race
        {
            Id = Guid.NewGuid(),
            Name = "Race 1 - Fleet A",
            Date = _season.Start.AddDays(1),
            ClubId = _clubId,
            Fleet = fleetA,
            Scores = new List<Database.Entities.Score>
            {
                new Database.Entities.Score { CompetitorId = compA1.Id, Place = 1 },
                new Database.Entities.Score { CompetitorId = compA2.Id, Place = 2 }
            }
        };

        var race2 = new Database.Entities.Race
        {
            Id = Guid.NewGuid(),
            Name = "Race 2 - Fleet B",
            Date = _season.Start.AddDays(2),
            ClubId = _clubId,
            Fleet = fleetB,
            Scores = new List<Database.Entities.Score>
            {
                new Database.Entities.Score { CompetitorId = compB1.Id, Place = 1 },
                new Database.Entities.Score { CompetitorId = compB2.Id, Place = 2 }
            }
        };

        _context.Races.AddRange(race1, race2);

        var scoringSystem = CreateAppendixAScoringSystem();

        // Create series filtered to Fleet A only, with UseFullRaceScores = false
        var series = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A Series",
            UrlName = "fleet-a-series",
            ClubId = _clubId,
            Season = _season,
            Type = Database.Entities.SeriesType.Standard,
            FleetId = fleetA.Id,
            UseFullRaceScores = false,
            ScoringSystem = scoringSystem,
            ScoringSystemId = scoringSystem.Id,
            RaceSeries = new List<Database.Entities.SeriesRace>
            {
                new Database.Entities.SeriesRace { RaceId = race1.Id },
                new Database.Entities.SeriesRace { RaceId = race2.Id }
            }
        };

        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        // Verify series was saved correctly
        var dbSeries = await _context.Series.FirstAsync(s => s.Id == series.Id);
        Assert.NotNull(dbSeries.ScoringSystemId);
        Assert.Equal(scoringSystem.Id, dbSeries.ScoringSystemId);

        // Verify scoring system exists and has proper data
        var scoringSystemFromDb = await _context.ScoringSystems.FirstAsync(s => s.Id == scoringSystem.Id);
        Assert.NotNull(scoringSystemFromDb);
        Assert.NotNull(scoringSystemFromDb.Name);

        // Act - Update series results with real Appendix A calculator
        await _service.UpdateSeriesResults(series.Id, "test");

        // Assert - Only Fleet A competitors should be in results
        var result = await _service.GetOneSeriesAsync(series.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.FlatResults);
        Assert.NotNull(result.FlatResults.Competitors);

        // Should only have 2 competitors from Fleet A
        Assert.Equal(2, result.FlatResults.Competitors.Count());
        Assert.Contains(result.FlatResults.Competitors, c => c.Id == compA1.Id);
        Assert.Contains(result.FlatResults.Competitors, c => c.Id == compA2.Id);
        Assert.DoesNotContain(result.FlatResults.Competitors, c => c.Id == compB1.Id);
        Assert.DoesNotContain(result.FlatResults.Competitors, c => c.Id == compB2.Id);
    }

    [Fact]
    public async Task FleetFiltering_UseFullRaceScoresFalse_RecalculatesScoresWithinFleet()
    {
        // Arrange - Create a multi-fleet race where scores should be recalculated for fleet
        var fleetA = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        var fleetB = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet B",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        _context.Fleets.Add(fleetA);
        _context.Fleets.Add(fleetB);

        // Create 3 competitors - 2 in Fleet A, 1 in Fleet B
        var compA1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A1",
            BoatName = "Boat A1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compA2 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A2",
            BoatName = "Boat A2",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compB1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "B1",
            BoatName = "Boat B1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetB.Id }
            }
        };

        _context.Competitors.AddRange(compA1, compA2, compB1);

        // Create mixed fleet race where B1 finishes between A1 and A2
        // Original places: A1=1, B1=2, A2=3
        // For Fleet A series, should recalculate to: A1=1, A2=2
        var race1 = new Database.Entities.Race
        {
            Id = Guid.NewGuid(),
            Name = "Mixed Fleet Race",
            Date = _season.Start.AddDays(1),
            ClubId = _clubId,
            Fleet = fleetA, // Race is associated with Fleet A
            Scores = new List<Database.Entities.Score>
            {
                new Database.Entities.Score { CompetitorId = compA1.Id, Place = 1 },
                new Database.Entities.Score { CompetitorId = compB1.Id, Place = 2 }, // Fleet B competitor in middle
                new Database.Entities.Score { CompetitorId = compA2.Id, Place = 3 }
            }
        };

        _context.Races.Add(race1);

        var scoringSystem = CreateAppendixAScoringSystem();

        // Create Fleet A series with UseFullRaceScores = false (recalculate)
        var series = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A Series",
            UrlName = "fleet-a-recalc-test",
            ClubId = _clubId,
            Season = _season,
            Type = Database.Entities.SeriesType.Standard,
            FleetId = fleetA.Id,
            UseFullRaceScores = false, // Should recalculate scores
            ScoringSystemId = scoringSystem.Id,
            RaceSeries = new List<Database.Entities.SeriesRace>
            {
                new Database.Entities.SeriesRace { RaceId = race1.Id }
            }
        };

        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateSeriesResults(series.Id, "test");

        // Assert
        var result = await _service.GetOneSeriesAsync(series.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.FlatResults);
        Assert.Equal(2, result.FlatResults.Competitors.Count());

        // Check calculated scores - should be recalculated as 1 and 2, not original 1 and 3
        var comp1Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA1.Id);
        var comp2Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA2.Id);

        Assert.Equal(1m, comp1Score.TotalScore); // A1 should have score of 1
        Assert.Equal(2m, comp2Score.TotalScore); // A2 should have recalculated score of 2 (not 3)
    }

    [Fact]
    public async Task FleetFiltering_UseFullRaceScoresTrue_PreservesOriginalPlaces()
    {
        // Arrange - Same setup as previous test but with UseFullRaceScores = true
        var fleetA = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        var fleetB = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet B",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        _context.Fleets.Add(fleetA);
        _context.Fleets.Add(fleetB);

        var compA1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A1",
            BoatName = "Boat A1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compA2 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A2",
            BoatName = "Boat A2",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compB1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "B1",
            BoatName = "Boat B1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetB.Id }
            }
        };

        _context.Competitors.AddRange(compA1, compA2, compB1);

        // Mixed fleet race: A1=1, B1=2, A2=3
        var race1 = new Database.Entities.Race
        {
            Id = Guid.NewGuid(),
            Name = "Mixed Fleet Race",
            Date = _season.Start.AddDays(1),
            ClubId = _clubId,
            Fleet = fleetA,
            Scores = new List<Database.Entities.Score>
            {
                new Database.Entities.Score { CompetitorId = compA1.Id, Place = 1 },
                new Database.Entities.Score { CompetitorId = compB1.Id, Place = 2 },
                new Database.Entities.Score { CompetitorId = compA2.Id, Place = 3 }
            }
        };

        _context.Races.Add(race1);

        var scoringSystem = CreateAppendixAScoringSystem();

        // Create Fleet A series with UseFullRaceScores = true (preserve original)
        var series = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A Series Full Scores",
            UrlName = "fleet-a-full-scores",
            ClubId = _clubId,
            Season = _season,
            Type = Database.Entities.SeriesType.Standard,
            FleetId = fleetA.Id,
            UseFullRaceScores = true, // Should preserve original places
            ScoringSystem = scoringSystem,
            ScoringSystemId = scoringSystem.Id,
            RaceSeries = new List<Database.Entities.SeriesRace>
            {
                new Database.Entities.SeriesRace { RaceId = race1.Id }
            }
        };

        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateSeriesResults(series.Id, "test");

        // Assert
        var result = await _service.GetOneSeriesAsync(series.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.FlatResults);
        Assert.Equal(2, result.FlatResults.Competitors.Count());

        // Check calculated scores - should preserve original places 1 and 3
        var comp1Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA1.Id);
        var comp2Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA2.Id);

        Assert.Equal(1m, comp1Score.TotalScore); // A1 should have score of 1
        Assert.Equal(3m, comp2Score.TotalScore); // A2 should have original score of 3 (not recalculated)
    }

    [Fact]
    public async Task FleetFiltering_WithCodedScoresAndRecalculation_HandlesCorrectly()
    {
        // Arrange - Test DNC scores with fleet filtering and recalculation
        var fleetA = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        var fleetB = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet B",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        _context.Fleets.Add(fleetA);
        _context.Fleets.Add(fleetB);

        var compA1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A1",
            BoatName = "Boat A1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compA2 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A2",
            BoatName = "Boat A2",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compB1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "B1",
            BoatName = "Boat B1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetB.Id }
            }
        };

        _context.Competitors.AddRange(compA1, compA2, compB1);

        // Race with A1=1st, A2=DNC, B1=2nd
        // In Fleet A series with recalc, A2's DNC should be calculated as number of Fleet A competitors + 1 = 3
        var race1 = new Database.Entities.Race
        {
            Id = Guid.NewGuid(),
            Name = "Race with DNC",
            Date = _season.Start.AddDays(1),
            ClubId = _clubId,
            Fleet = fleetA,
            Scores = new List<Database.Entities.Score>
            {
                new Database.Entities.Score { CompetitorId = compA1.Id, Place = 1 },
                new Database.Entities.Score { CompetitorId = compB1.Id, Place = 2 },
                new Database.Entities.Score { CompetitorId = compA2.Id, Code = "DNC" }
            }
        };

        _context.Races.Add(race1);

        var scoringSystem = CreateAppendixAScoringSystem();

        var series = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A Series with DNC",
            UrlName = "fleet-a-dnc-test",
            ClubId = _clubId,
            Season = _season,
            Type = Database.Entities.SeriesType.Standard,
            FleetId = fleetA.Id,
            UseFullRaceScores = false,
            ScoringSystemId = scoringSystem.Id,
            RaceSeries = new List<Database.Entities.SeriesRace>
            {
                new Database.Entities.SeriesRace { RaceId = race1.Id }
            }
        };

        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateSeriesResults(series.Id, "test");

        // Assert
        var result = await _service.GetOneSeriesAsync(series.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.FlatResults);
        Assert.Equal(2, result.FlatResults.Competitors.Count()); // Only Fleet A competitors

        var comp1Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA1.Id);
        var comp2Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA2.Id);

        Assert.Equal(1m, comp1Score.TotalScore); // A1 finished 1st
        // A2's DNC should be calculated as: number of fleet competitors + 1 = 2 + 1 = 3
        Assert.Equal(3m, comp2Score.TotalScore);
    }

    [Fact]
    public async Task FleetFiltering_MultipleRacesSameFleet_CorrectTotals()
    {
        // Arrange - Multiple races in same fleet with consistent scoring
        var fleetA = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        _context.Fleets.Add(fleetA);

        var compA1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A1",
            BoatName = "Boat A1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compA2 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A2",
            BoatName = "Boat A2",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compA3 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A3",
            BoatName = "Boat A3",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };

        _context.Competitors.AddRange(compA1, compA2, compA3);

        // Race 1: A1=1, A2=2, A3=3
        var race1 = new Database.Entities.Race
        {
            Id = Guid.NewGuid(),
            Name = "Race 1",
            Date = _season.Start.AddDays(1),
            ClubId = _clubId,
            Fleet = fleetA,
            Scores = new List<Database.Entities.Score>
            {
                new Database.Entities.Score { CompetitorId = compA1.Id, Place = 1 },
                new Database.Entities.Score { CompetitorId = compA2.Id, Place = 2 },
                new Database.Entities.Score { CompetitorId = compA3.Id, Place = 3 }
            }
        };

        // Race 2: A2=1, A1=2, A3=3
        var race2 = new Database.Entities.Race
        {
            Id = Guid.NewGuid(),
            Name = "Race 2",
            Date = _season.Start.AddDays(2),
            ClubId = _clubId,
            Fleet = fleetA,
            Scores = new List<Database.Entities.Score>
            {
                new Database.Entities.Score { CompetitorId = compA2.Id, Place = 1 },
                new Database.Entities.Score { CompetitorId = compA1.Id, Place = 2 },
                new Database.Entities.Score { CompetitorId = compA3.Id, Place = 3 }
            }
        };

        // Race 3: A3=1, A1=2, A2=3
        var race3 = new Database.Entities.Race
        {
            Id = Guid.NewGuid(),
            Name = "Race 3",
            Date = _season.Start.AddDays(3),
            ClubId = _clubId,
            Fleet = fleetA,
            Scores = new List<Database.Entities.Score>
            {
                new Database.Entities.Score { CompetitorId = compA3.Id, Place = 1 },
                new Database.Entities.Score { CompetitorId = compA1.Id, Place = 2 },
                new Database.Entities.Score { CompetitorId = compA2.Id, Place = 3 }
            }
        };

        _context.Races.AddRange(race1, race2, race3);

        var scoringSystem = CreateAppendixAScoringSystem();
        scoringSystem.DiscardPattern = "0,0,1"; // Discard worst after 3 races

        var series = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A Multi-Race Series",
            UrlName = "fleet-a-multi-race",
            ClubId = _clubId,
            Season = _season,
            Type = Database.Entities.SeriesType.Standard,
            FleetId = fleetA.Id,
            UseFullRaceScores = false,
            ScoringSystemId = scoringSystem.Id,
            RaceSeries = new List<Database.Entities.SeriesRace>
            {
                new Database.Entities.SeriesRace { RaceId = race1.Id },
                new Database.Entities.SeriesRace { RaceId = race2.Id },
                new Database.Entities.SeriesRace { RaceId = race3.Id }
            }
        };

        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateSeriesResults(series.Id, "test");

        // Assert
        var result = await _service.GetOneSeriesAsync(series.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.FlatResults);
        Assert.Equal(3, result.FlatResults.Competitors.Count());

        var comp1Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA1.Id);
        var comp2Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA2.Id);
        var comp3Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA3.Id);

        // A1: 1+2+2=5, discard 2, total=3
        // A2: 2+1+3=6, discard 3, total=3
        // A3: 3+3+1=7, discard 3, total=4
        Assert.Equal(3m, comp1Score.TotalScore);
        Assert.Equal(3m, comp2Score.TotalScore);
        Assert.Equal(4m, comp3Score.TotalScore);

        // Verify ranking (lower is better in low point)
        Assert.True(comp3Score.Rank >= comp1Score.Rank);
        Assert.True(comp3Score.Rank >= comp2Score.Rank);
    }

    [Fact]
    public async Task FleetFiltering_CrossFleetRaceScenario_ComplexRecalculation()
    {
        // Arrange - Complex scenario: Race has boats from both fleets,
        // Series is for Fleet A with recalculation
        var fleetA = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        var fleetB = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet B",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        _context.Fleets.Add(fleetA);
        _context.Fleets.Add(fleetB);

        // 3 in Fleet A, 2 in Fleet B
        var compA1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A1",
            BoatName = "Boat A1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compA2 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A2",
            BoatName = "Boat A2",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compA3 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A3",
            BoatName = "Boat A3",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compB1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "B1",
            BoatName = "Boat B1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetB.Id }
            }
        };
        var compB2 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "B2",
            BoatName = "Boat B2",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetB.Id }
            }
        };

        _context.Competitors.AddRange(compA1, compA2, compA3, compB1, compB2);

        // Race with interleaved finishes: A1, B1, A2, B2, A3
        // Original places: A1=1, B1=2, A2=3, B2=4, A3=5
        // For Fleet A series (recalc): A1=1, A2=2, A3=3
        var race1 = new Database.Entities.Race
        {
            Id = Guid.NewGuid(),
            Name = "Mixed Fleet Race",
            Date = _season.Start.AddDays(1),
            ClubId = _clubId,
            Fleet = fleetA,
            Scores = new List<Database.Entities.Score>
            {
                new Database.Entities.Score { CompetitorId = compA1.Id, Place = 1 },
                new Database.Entities.Score { CompetitorId = compB1.Id, Place = 2 },
                new Database.Entities.Score { CompetitorId = compA2.Id, Place = 3 },
                new Database.Entities.Score { CompetitorId = compB2.Id, Place = 4 },
                new Database.Entities.Score { CompetitorId = compA3.Id, Place = 5 }
            }
        };

        _context.Races.Add(race1);

        var scoringSystem = CreateAppendixAScoringSystem();

        var series = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A Series Complex",
            UrlName = "fleet-a-complex",
            ClubId = _clubId,
            Season = _season,
            Type = Database.Entities.SeriesType.Standard,
            FleetId = fleetA.Id,
            UseFullRaceScores = false,
            ScoringSystemId = scoringSystem.Id,
            RaceSeries = new List<Database.Entities.SeriesRace>
            {
                new Database.Entities.SeriesRace { RaceId = race1.Id }
            }
        };

        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateSeriesResults(series.Id, "test");

        // Assert
        var result = await _service.GetOneSeriesAsync(series.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.FlatResults);
        Assert.Equal(3, result.FlatResults.Competitors.Count());

        // Only Fleet A competitors should be included
        Assert.DoesNotContain(result.FlatResults.Competitors, c => c.Id == compB1.Id);
        Assert.DoesNotContain(result.FlatResults.Competitors, c => c.Id == compB2.Id);

        // Scores should be recalculated ignoring Fleet B finishes
        var comp1Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA1.Id);
        var comp2Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA2.Id);
        var comp3Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA3.Id);

        Assert.Equal(1m, comp1Score.TotalScore); // Recalculated: 1st
        Assert.Equal(2m, comp2Score.TotalScore); // Recalculated: 2nd (not 3rd)
        Assert.Equal(3m, comp3Score.TotalScore); // Recalculated: 3rd (not 5th)
    }


    [Fact]
    public async Task FleetFiltering_CrossFleetDnc_IncludesFleetOnly()
    {
        // Arrange - Complex scenario: Race has boats from both fleets,
        // Series is for Fleet A with recalculation
        var fleetA = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        var fleetB = new Database.Entities.Fleet
        {
            Id = Guid.NewGuid(),
            Name = "Fleet B",
            FleetType = Api.Enumerations.FleetType.SelectedBoats,
            ClubId = _clubId
        };
        _context.Fleets.Add(fleetA);
        _context.Fleets.Add(fleetB);

        // 4 in Fleet A, 3 in Fleet B
        var compA1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A1",
            BoatName = "Boat A1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compA2 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A2",
            BoatName = "Boat A2",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compA3 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A3",
            BoatName = "Boat A3",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compA4 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "A4",
            BoatName = "Boat A4",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetA.Id }
            }
        };
        var compB1 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "B1",
            BoatName = "Boat B1",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetB.Id }
            }
        };
        var compB2 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "B2",
            BoatName = "Boat B2",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetB.Id }
            }
        };
        var compB3 = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(),
            Name = "B3",
            BoatName = "Boat B3",
            ClubId = _clubId,
            BoatClassId = _boatClassId,
            CompetitorFleets = new List<Database.Entities.CompetitorFleet>
            {
                new Database.Entities.CompetitorFleet { FleetId = fleetB.Id }
            }
        };

        _context.Competitors.AddRange(compA1, compA2, compA3, compA4, compB1, compB2, compB3);

        // Race with interleaved finishes: A1, B1, A2, B2, A3
        // Original places: A1=1, B1=2, A2=3, B2=4, A3=5
        // For Fleet A series (recalc): A1=1, A2=2, A3=3
        var race1 = new Database.Entities.Race
        {
            Id = Guid.NewGuid(),
            Name = "Mixed Fleet Race",
            Date = _season.Start.AddDays(1),
            ClubId = _clubId,
            Fleet = fleetA,
            Scores = new List<Database.Entities.Score>
            {
                new Database.Entities.Score { CompetitorId = compA1.Id, Place = 1 },
                new Database.Entities.Score { CompetitorId = compB1.Id, Place = 2 },
                new Database.Entities.Score { CompetitorId = compA2.Id, Place = 3 },
                new Database.Entities.Score { CompetitorId = compB2.Id, Place = 4 },
                new Database.Entities.Score { CompetitorId = compA3.Id, Place = 5 },
                new Database.Entities.Score { CompetitorId = compB3.Id, Place = 6 },
                new Database.Entities.Score { CompetitorId = compA4.Id, Code = "DNC" },
            }
        };

        _context.Races.Add(race1);

        var scoringSystem = CreateAppendixAScoringSystem();

        var series = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            Name = "Fleet A Series Complex",
            UrlName = "fleet-a-complex",
            ClubId = _clubId,
            Season = _season,
            Type = Database.Entities.SeriesType.Standard,
            FleetId = fleetA.Id,
            UseFullRaceScores = false,
            ScoringSystemId = scoringSystem.Id,
            RaceSeries = new List<Database.Entities.SeriesRace>
            {
                new Database.Entities.SeriesRace { RaceId = race1.Id }
            }
        };

        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateSeriesResults(series.Id, "test");

        // Assert
        var result = await _service.GetOneSeriesAsync(series.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.FlatResults);
        Assert.Equal(4, result.FlatResults.Competitors.Count());

        // Only Fleet A competitors should be included
        Assert.DoesNotContain(result.FlatResults.Competitors, c => c.Id == compB1.Id);
        Assert.DoesNotContain(result.FlatResults.Competitors, c => c.Id == compB2.Id);

        // Scores should be recalculated ignoring Fleet B finishes
        var comp1Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA1.Id);
        var comp2Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA2.Id);
        var comp3Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA3.Id);
        var comp4Score = result.FlatResults.CalculatedScores.Single(s => s.CompetitorId == compA4.Id);

        Assert.Equal(5m, comp4Score.TotalScore); // DNC should be calculated as number of fleet competitors + 1 = 4 + 1 = 5
        Assert.Equal(1m, comp1Score.TotalScore); // Recalculated: 1st
        Assert.Equal(2m, comp2Score.TotalScore); // Recalculated: 2nd (not 3rd)
        Assert.Equal(3m, comp3Score.TotalScore); // Recalculated: 3rd (not 5th)


    }
}
