using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

/// <summary>
/// Phase 3 Implementation Tests: Position Recalculation for Fleet-Filtered Series
/// 
/// IMPORTANT: These tests are SPECIFICATION tests for Phase 3 implementation.
/// They WILL FAIL until the scoring calculator implements fleet-based position recalculation.
/// 
/// These tests call the actual scoring calculator and verify it produces correct results
/// when UseFullRaceScores = false with a fleet-filtered series.
/// 
/// Key principle: Race scores in the database are NEVER changed. Only the calculation of series
/// results is affected by fleet selection. The scoring calculator must recalculate positions
/// to only count fleet competitors when UseFullRaceScores = false.
/// </summary>
public class FleetPositionRecalculationTests
{
    private ScoringSystem _scoringSystem;

    public FleetPositionRecalculationTests()
    {
        _scoringSystem = MakeDefaultScoringSystem();
    }

    private ScoringSystem MakeDefaultScoringSystem()
    {
        var system = new ScoringSystem
        {
            Id = Guid.NewGuid(),
            Name = "Appendix A Low Point (Fleet Test)",
            DiscardPattern = "0,1",
            ParentSystemId = null
        };

        system.InheritedScoreCodes = new List<ScoreCode>();
        system.ScoreCodes = new List<ScoreCode>();

        return system;
    }

    private Competitor CreateCompetitor(string name, Guid fleetId, Guid clubId)
    {
        return new Competitor
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = name,
            IsActive = true,
            Fleets = new List<Fleet> { new Fleet { Id = fleetId, Name = "Test Fleet", ClubId = clubId } }
        };
    }

    private Fleet CreateFleet(string name, Guid clubId)
    {
        return new Fleet
        {
            Id = Guid.NewGuid(),
            Name = name,
            ClubId = clubId,
            IsActive = true
        };
    }

    [Fact]
    public void ScoringCalculator_WithFleetAndUseFullRaceScoresFalse_RecalculatesPositionsByFleet()
    {
        // This test SPECIFIES what the scoring calculator MUST do in Phase 3
        // It will FAIL until the calculator implements fleet-based position recalculation

        var clubId = Guid.NewGuid();
        var womensFleet = CreateFleet("Women's Fleet", clubId);
        var mensFleet = CreateFleet("Men's Fleet", clubId);

        // Create competitors in different fleets
        var compA = CreateCompetitor("Woman A", womensFleet.Id, clubId); // 4th overall
        var compB = CreateCompetitor("Woman B", womensFleet.Id, clubId); // 2nd overall
        var compC = CreateCompetitor("Woman C", womensFleet.Id, clubId); // 6th overall
        var compD = CreateCompetitor("Man D", mensFleet.Id, clubId);     // 1st overall
        var compE = CreateCompetitor("Man E", mensFleet.Id, clubId);     // 3rd overall

        var raceId = Guid.NewGuid();
        var race = new Race
        {
            Id = raceId,
            Name = "Mixed Fleet Race",
            Date = DateTime.Now,
            ClubId = clubId,
            Fleet = womensFleet,
        };
        race.Scores = new List<Score>
        {
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = compD.Id, Competitor = compD, Place = 1 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = compB.Id, Competitor = compB, Place = 2 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = compE.Id, Competitor = compE, Place = 3 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = compA.Id, Competitor = compA, Place = 4 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = compC.Id, Competitor = compC, Place = 6 },
        };

        var series = new Series
        {
            Id = Guid.NewGuid(),
            Name = "Women's Championship",
            ClubId = clubId,
            FleetId = womensFleet.Id,           // Filter to Women's Fleet
            UseFullRaceScores = false,          // Recalculate positions by fleet ONLY
            ScoringSystem = _scoringSystem,
            Races = new List<Race> { race },
            Competitors = new List<Competitor> { compA, compB, compC } // Only women in results
        };

        // Act
        var calculator = new AppendixACalculator(_scoringSystem);
        var results = calculator.CalculateResults(series);

        // Assert
        // When UseFullRaceScores=false, positions should be recalculated based on women-only competitors:
        // - Woman B (2nd overall) → 1st among women  → scores 1
        // - Woman A (4th overall) → 2nd among women  → scores 2
        // - Woman C (6th overall) → 3rd among women  → scores 3
        Assert.NotNull(results);
        Assert.Equal(3, results.Results.Count); // Only 3 women in results

        var compAResult = results.Results.First(r => r.Key.Id == compA.Id).Value;
        var compBResult = results.Results.First(r => r.Key.Id == compB.Id).Value;
        var compCResult = results.Results.First(r => r.Key.Id == compC.Id).Value;

        Assert.Equal(2m, compAResult.TotalScore); // Was 4th overall, now 2nd among women
        Assert.Equal(1m, compBResult.TotalScore); // Was 2nd overall, now 1st among women
        Assert.Equal(3m, compCResult.TotalScore); // Was 6th overall, now 3rd among women
    }

    [Fact]
    public void ScoringCalculator_WithFleetAndUseFullRaceScoresTrue_UsesOriginalPositions()
    {
        // This test SPECIFIES that when UseFullRaceScores=true, original positions should be used
        // It will FAIL until the calculator implements this logic

        var clubId = Guid.NewGuid();
        var womensFleet = CreateFleet("Women's Fleet", clubId);
        var mensFleet = CreateFleet("Men's Fleet", clubId);

        var compA = CreateCompetitor("Woman A", womensFleet.Id, clubId);
        var compB = CreateCompetitor("Woman B", womensFleet.Id, clubId);
        var compC = CreateCompetitor("Woman C", womensFleet.Id, clubId);
        var compD = CreateCompetitor("Man D", mensFleet.Id, clubId);
        var compE = CreateCompetitor("Man E", mensFleet.Id, clubId);

        var raceId = Guid.NewGuid();
        var race = new Race
        {
            Id = raceId,
            Name = "Mixed Fleet Race",
            Date = DateTime.Now,
            ClubId = clubId,
            Fleet = womensFleet,
        };
        race.Scores = new List<Score>
        {
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = compD.Id, Competitor = compD, Place = 1 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = compB.Id, Competitor = compB, Place = 2 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = compE.Id, Competitor = compE, Place = 3 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = compA.Id, Competitor = compA, Place = 4 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = compC.Id, Competitor = compC, Place = 6 },
        };

        var series = new Series
        {
            Id = Guid.NewGuid(),
            Name = "Women's Championship",
            ClubId = clubId,
            FleetId = womensFleet.Id,
            UseFullRaceScores = true,  // USE ORIGINAL positions — don't recalculate
            ScoringSystem = _scoringSystem,
            Races = new List<Race> { race },
            Competitors = new List<Competitor> { compA, compB, compC }
        };

        // Act
        var calculator = new AppendixACalculator(_scoringSystem);
        var results = calculator.CalculateResults(series);

        // Assert — original race positions should be used as-is
        Assert.NotNull(results);
        Assert.Equal(3, results.Results.Count);

        var compAResult = results.Results.First(r => r.Key.Id == compA.Id).Value;
        var compBResult = results.Results.First(r => r.Key.Id == compB.Id).Value;
        var compCResult = results.Results.First(r => r.Key.Id == compC.Id).Value;

        Assert.Equal(4m, compAResult.TotalScore); // Stays 4th (original position)
        Assert.Equal(2m, compBResult.TotalScore); // Stays 2nd (original position)
        Assert.Equal(6m, compCResult.TotalScore); // Stays 6th (original position)
    }

    [Fact]
    public void ScoringCalculator_WithoutFleet_IgnoresUseFullRaceScoresSetting()
    {
        // This test SPECIFIES that UseFullRaceScores has no effect when FleetId is null

        var clubId = Guid.NewGuid();
        var fleet1 = CreateFleet("Fleet 1", clubId);
        var fleet2 = CreateFleet("Fleet 2", clubId);

        var comp1 = CreateCompetitor("Comp 1", fleet1.Id, clubId);
        var comp2 = CreateCompetitor("Comp 2", fleet2.Id, clubId);
        var comp3 = CreateCompetitor("Comp 3", fleet1.Id, clubId);

        var raceId = Guid.NewGuid();
        var race = new Race
        {
            Id = raceId,
            Name = "Test Race",
            Date = DateTime.Now,
            ClubId = clubId,
            Fleet = fleet1,
        };
        race.Scores = new List<Score>
        {
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = comp2.Id, Competitor = comp2, Place = 1 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = comp3.Id, Competitor = comp3, Place = 2 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = comp1.Id, Competitor = comp1, Place = 4 },
        };

        var series = new Series
        {
            Id = Guid.NewGuid(),
            Name = "Open Championship",
            ClubId = clubId,
            FleetId = null,                  // NO FLEET FILTER
            UseFullRaceScores = false,       // This setting should be ignored
            ScoringSystem = _scoringSystem,
            Races = new List<Race> { race },
            Competitors = new List<Competitor> { comp1, comp2, comp3 }
        };

        // Act
        var calculator = new AppendixACalculator(_scoringSystem);
        var results = calculator.CalculateResults(series);

        // Assert — original positions used for all competitors (no filtering)
        Assert.NotNull(results);
        Assert.Equal(3, results.Results.Count);

        var comp1Result = results.Results.First(r => r.Key.Id == comp1.Id).Value;
        Assert.Equal(4m, comp1Result.TotalScore); // Still scored at position 4
    }

    [Fact]
    public void ScoringCalculator_RaceWithNoFleetCompetitors_HandlesGracefully()
    {
        // A race is part of the series but has no fleet competitors.
        // Fleet competitors should receive a DNC for that race — the calculator must not crash.

        var clubId = Guid.NewGuid();
        var womensFleet = CreateFleet("Women's Fleet", clubId);
        var mensFleet = CreateFleet("Men's Fleet", clubId);

        var womanA = CreateCompetitor("Woman A", womensFleet.Id, clubId);
        var manX = CreateCompetitor("Man X", mensFleet.Id, clubId);
        var manY = CreateCompetitor("Man Y", mensFleet.Id, clubId);

        var raceId = Guid.NewGuid();
        var race = new Race
        {
            Id = raceId,
            Name = "Race with No Women",
            Date = DateTime.Now,
            ClubId = clubId,
            Fleet = mensFleet,
        };
        race.Scores = new List<Score>
        {
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = manX.Id, Competitor = manX, Place = 1 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = manY.Id, Competitor = manY, Place = 2 },
        };

        var series = new Series
        {
            Id = Guid.NewGuid(),
            Name = "Women's Series",
            ClubId = clubId,
            FleetId = womensFleet.Id,
            UseFullRaceScores = false,
            ScoringSystem = _scoringSystem,
            Races = new List<Race> { race },
            Competitors = new List<Competitor> { womanA }
        };

        // Act
        var calculator = new AppendixACalculator(_scoringSystem);
        var exception = Record.Exception(() => calculator.CalculateResults(series));

        // Assert — no crash; womanA should receive a DNC for this race
        Assert.Null(exception);
    }

    [Fact]
    public void ScoringCalculator_MixedFleetRace_RanksOnlyFleetCompetitors()
    {
        // Complex scenario: 4 women and 3 men interleaved in one race.
        // Race order: M1(1st), W1(2nd), M2(3rd), W2(4th), W3(5th), M3(6th), W4(7th)
        // Expected fleet positions: W1→1, W2→2, W3→3, W4→4

        var clubId = Guid.NewGuid();
        var womensFleet = CreateFleet("Women's Fleet", clubId);
        var mensFleet = CreateFleet("Men's Fleet", clubId);

        var women = Enumerable.Range(1, 4)
            .Select(i => CreateCompetitor($"Woman {i}", womensFleet.Id, clubId))
            .ToList();

        var men = Enumerable.Range(1, 3)
            .Select(i => CreateCompetitor($"Man {i}", mensFleet.Id, clubId))
            .ToList();

        var raceId = Guid.NewGuid();
        var race = new Race
        {
            Id = raceId,
            Name = "Mixed Race",
            Date = DateTime.Now,
            ClubId = clubId,
            Fleet = womensFleet,
        };
        race.Scores = new List<Score>
        {
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = men[0].Id, Competitor = men[0], Place = 1 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = women[0].Id, Competitor = women[0], Place = 2 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = men[1].Id, Competitor = men[1], Place = 3 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = women[1].Id, Competitor = women[1], Place = 4 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = women[2].Id, Competitor = women[2], Place = 5 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = men[2].Id, Competitor = men[2], Place = 6 },
            new Score { Id = Guid.NewGuid(), RaceId = raceId, Race = race, CompetitorId = women[3].Id, Competitor = women[3], Place = 7 },
        };

        var series = new Series
        {
            Id = Guid.NewGuid(),
            Name = "Women's Championship",
            ClubId = clubId,
            FleetId = womensFleet.Id,
            UseFullRaceScores = false,
            ScoringSystem = _scoringSystem,
            Races = new List<Race> { race },
            Competitors = women
        };

        // Act
        var calculator = new AppendixACalculator(_scoringSystem);
        var results = calculator.CalculateResults(series);

        // Assert — scored by fleet-only ranking, not overall race positions
        Assert.NotNull(results);
        Assert.Equal(4, results.Results.Count); // Only women

        Assert.Equal(1m, results.Results.First(r => r.Key.Id == women[0].Id).Value.TotalScore); // W1: 2nd overall → 1st among women
        Assert.Equal(2m, results.Results.First(r => r.Key.Id == women[1].Id).Value.TotalScore); // W2: 4th overall → 2nd among women
        Assert.Equal(3m, results.Results.First(r => r.Key.Id == women[2].Id).Value.TotalScore); // W3: 5th overall → 3rd among women
        Assert.Equal(4m, results.Results.First(r => r.Key.Id == women[3].Id).Value.TotalScore); // W4: 7th overall → 4th among women
    }
}
