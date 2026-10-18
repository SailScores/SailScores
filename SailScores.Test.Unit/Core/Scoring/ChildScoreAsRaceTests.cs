using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

public class ChildScoreAsRaceTests
{
    private static readonly Competitor A = new() { Id = Guid.NewGuid(), Name = "A" };
    private static readonly Competitor B = new() { Id = Guid.NewGuid(), Name = "B" };
    private static readonly Competitor C = new() { Id = Guid.NewGuid(), Name = "C" };

    private static Series MakeChild(string name, DateTime date) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        UrlName = name,
        Season = new Season { UrlName = "2026" },
        Races = new List<Race> { new() { Id = Guid.NewGuid(), Date = date, State = RaceState.Raced } }
    };

    private static FlatSeriesScore Result(Competitor c, int? rank, decimal? total, decimal? average = null) => new()
    {
        CompetitorId = c.Id,
        Rank = rank,
        TotalScore = total,
        Average = average
    };

    private static FlatResults Results(params FlatSeriesScore[] scores) => new() { CalculatedScores = scores };

    private static ScoringSystem MakeSystem() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test",
        DiscardPattern = "0",
        ScoreCodes = new List<ScoreCode>
        {
            new()
            {
                Name = "DNC", Formula = "SER+", FormulaValue = 1m, CameToStart = false,
                Started = false, Finished = false, Discardable = true
            }
        },
        InheritedScoreCodes = new List<ScoreCode>()
    };

    // Child 1: A 10 (1st), B 14 (2nd), C unranked with an average of 3.5.
    // Child 2: A 12 (2nd), B 9 (1st), C 20 (3rd).
    private static Series MakeSummary(bool includeUnranked)
    {
        var child1 = new SeriesAsRace(
            MakeChild("child-1", new DateTime(2026, 6, 6)),
            Results(Result(A, 1, 10m), Result(B, 2, 14m), Result(C, null, null, 3.5m)),
            SummaryRollupMode.ChildScoreAsRace,
            includeUnranked);
        var child2 = new SeriesAsRace(
            MakeChild("child-2", new DateTime(2026, 6, 13)),
            Results(Result(A, 2, 12m), Result(B, 1, 9m), Result(C, 3, 20m)),
            SummaryRollupMode.ChildScoreAsRace,
            includeUnranked);
        var competitors = new List<Competitor> { A, B, C };
        foreach (var score in child1.Scores.Concat(child2.Scores))
        {
            score.Competitor = competitors.Single(c => c.Id == score.CompetitorId);
        }
        return new Series
        {
            Id = Guid.NewGuid(),
            Name = "Summary",
            Races = new List<Race> { child1, child2 },
            Competitors = competitors,
            ScoringSystem = MakeSystem()
        };
    }

    [Fact]
    public void SeriesAsRace_ChildScoreAsRace_PresetsChildSeriesScore()
    {
        var race = new SeriesAsRace(
            MakeChild("child", new DateTime(2026, 6, 6)),
            Results(Result(A, 1, 10m), Result(B, 2, null, 4.25m)),
            SummaryRollupMode.ChildScoreAsRace);

        Assert.Equal(10m, race.Scores.Single(s => s.CompetitorId == A.Id).PresetScoreValue);
        Assert.Equal(4.25m, race.Scores.Single(s => s.CompetitorId == B.Id).PresetScoreValue);
    }

    [Fact]
    public void SeriesAsRace_UnrankedNotIncluded_LeavesCompetitorOut()
    {
        var race = new SeriesAsRace(
            MakeChild("child", new DateTime(2026, 6, 6)),
            Results(Result(A, 1, 10m), Result(C, null, null, 3.5m)),
            SummaryRollupMode.ChildScoreAsRace,
            includeUnrankedScores: false);

        Assert.DoesNotContain(race.Scores, s => s.CompetitorId == C.Id);
    }

    [Fact]
    public void SeriesAsRace_UnrankedIncluded_UsesUnrankedScore()
    {
        var race = new SeriesAsRace(
            MakeChild("child", new DateTime(2026, 6, 6)),
            Results(Result(A, 1, 10m), Result(C, null, null, 3.5m)),
            SummaryRollupMode.ChildScoreAsRace,
            includeUnrankedScores: true);

        var score = race.Scores.Single(s => s.CompetitorId == C.Id);
        Assert.Null(score.Place);
        Assert.Equal(3.5m, score.PresetScoreValue);
    }

    [Fact]
    public void SeriesAsRace_ChildRankAsRace_UsesRankAsPlace()
    {
        var race = new SeriesAsRace(
            MakeChild("child", new DateTime(2026, 6, 6)),
            Results(Result(A, 2, 10m), Result(C, null, null, 3.5m)));

        var score = Assert.Single(race.Scores);
        Assert.Equal(2, score.Place);
        Assert.Null(score.PresetScoreValue);
    }

    [Fact]
    public void CalculateResults_AppendixA_TotalsChildScoresWithoutCapping()
    {
        // Child totals are far above a DNC (4 points here) and must not be capped to it.
        var series = MakeSummary(includeUnranked: false);

        var results = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.AppendixA, series.ScoringSystem).CalculateResults(series);

        Assert.Equal(22m, results.Results[A].TotalScore);
        Assert.Equal(23m, results.Results[B].TotalScore);
        Assert.Equal(1, results.Results[A].Rank);
        // C wasn't ranked in child 1, so gets the default DNC score (3 competitors + 1) there.
        Assert.Equal(4m, results.Results[C].CalculatedScores[series.Races[0]].ScoreValue);
        Assert.Equal(24m, results.Results[C].TotalScore);
    }

    [Fact]
    public void CalculateResults_AppendixAIncludingUnranked_UsesUnrankedChildScore()
    {
        var series = MakeSummary(includeUnranked: true);

        var results = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.AppendixA, series.ScoringSystem).CalculateResults(series);

        Assert.Equal(3.5m, results.Results[C].CalculatedScores[series.Races[0]].ScoreValue);
        Assert.Equal(23.5m, results.Results[C].TotalScore);
    }

    [Fact]
    public void CalculateResults_LowPointAverage_AveragesChildScoresAndCountsParticipation()
    {
        var series = MakeSummary(includeUnranked: true);
        series.ScoringSystem.ParticipationPercent = 100m;

        var results = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.LowPointAverageExclDnc, series.ScoringSystem).CalculateResults(series);

        Assert.Equal(11m, results.Results[A].TotalScore);
        // The unranked child score counts as sailing that child series.
        Assert.Equal(11.75m, results.Results[C].TotalScore);
    }
    // A plain child series: A and B sail three races; DNC is series competitors + 1 = 3.
    private static Series MakeChildWithRaces(string discardPattern)
    {
        var races = new List<Race>();
        for (int i = 0; i < 3; i++)
        {
            var race = new Race
            {
                Id = Guid.NewGuid(), Name = $"Race {i + 1}", Order = i + 1,
                Date = new DateTime(2026, 6, 6), State = RaceState.Raced, Scores = new List<Score>()
            };
            race.Scores.Add(new Score { Competitor = A, CompetitorId = A.Id, Race = race, Place = 1 });
            race.Scores.Add(new Score { Competitor = B, CompetitorId = B.Id, Race = race, Place = 2 });
            races.Add(race);
        }
        var system = MakeSystem();
        system.DiscardPattern = discardPattern;
        return new Series
        {
            Id = Guid.NewGuid(), Name = "Child", Races = races,
            Competitors = new List<Competitor> { A, B }, ScoringSystem = system
        };
    }

    [Fact]
    public void CalculateAbsentCompetitorScore_AppendixA_SumsChildDncsWithoutCountingAbsentCompetitor()
    {
        var child = MakeChildWithRaces("0");

        var score = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.AppendixA, child.ScoringSystem).CalculateAbsentCompetitorScore(child);

        // 3 races x (2 competitors + 1), not (3 competitors + 1).
        Assert.Equal(9m, score);
    }

    [Fact]
    public void CalculateAbsentCompetitorScore_WithDiscards_DiscardsLikeAnyCompetitor()
    {
        var child = MakeChildWithRaces("0,0,1");

        var score = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.AppendixA, child.ScoringSystem).CalculateAbsentCompetitorScore(child);

        Assert.Equal(6m, score);
    }

    [Fact]
    public void CalculateAbsentCompetitorScore_DoesNotChangeChildResults()
    {
        var child = MakeChildWithRaces("0");
        var calculator = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.AppendixA, child.ScoringSystem);

        calculator.CalculateAbsentCompetitorScore(child);
        var results = calculator.CalculateResults(child);

        Assert.Equal(2, results.Competitors.Count);
        Assert.Equal(6m, results.Results[B].TotalScore);
    }

    [Fact]
    public void CalculateAbsentCompetitorScore_LowPointAverageExcludingDncs_ReturnsNull()
    {
        var child = MakeChildWithRaces("0");

        var score = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.LowPointAverageExclDnc, child.ScoringSystem).CalculateAbsentCompetitorScore(child);

        Assert.Null(score);
    }

    [Fact]
    public void CalculateResults_AbsentCompetitorScore_UsedAsDncWithoutCapping()
    {
        var series = MakeSummary(includeUnranked: false);
        ((SeriesAsRace)series.Races[0]).AddAbsentCompetitorScores(series.Competitors.Select(c => c.Id), 30m);
        foreach (var score in series.Races[0].Scores.Where(s => s.Competitor == null))
        {
            score.Competitor = series.Competitors.Single(c => c.Id == score.CompetitorId);
        }

        var results = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.AppendixA, series.ScoringSystem).CalculateResults(series);

        var cScore = results.Results[C].CalculatedScores[series.Races[0]];
        Assert.Equal("DNC", cScore.RawScore.Code);
        Assert.Equal(30m, cScore.ScoreValue);
        Assert.Equal(50m, results.Results[C].TotalScore);
        // Competitors with a child score keep it.
        Assert.Equal(22m, results.Results[A].TotalScore);
    }

    [Fact]
    public void CalculateResults_AbsentScoreInLowPointAverageExcludingDncs_IsLeftOut()
    {
        var series = MakeSummary(includeUnranked: false);
        ((SeriesAsRace)series.Races[0]).AddAbsentCompetitorScores(series.Competitors.Select(c => c.Id), 30m);
        foreach (var score in series.Races[0].Scores.Where(s => s.Competitor == null))
        {
            score.Competitor = series.Competitors.Single(c => c.Id == score.CompetitorId);
        }

        var results = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.LowPointAverageExclDnc, series.ScoringSystem).CalculateResults(series);

        Assert.Equal(20m, results.Results[C].Average);
        Assert.Equal(50m, results.Results[C].ParticipationPercent);
    }
}
