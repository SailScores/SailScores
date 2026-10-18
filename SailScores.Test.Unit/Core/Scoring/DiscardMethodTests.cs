using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

public class DiscardMethodTests
{
    private static readonly DateTime Day1 = new(2026, 6, 6);
    private static readonly DateTime Day2 = new(2026, 6, 7);
    private static readonly Guid Child1 = Guid.NewGuid();
    private static readonly Guid Child2 = Guid.NewGuid();

    private static ScoringSystem MakeSystem(DiscardMethod method, string pattern = "0,0,1") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test",
        DiscardPattern = pattern,
        EffectiveDiscardMethod = method,
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

    // Six races, three on each day (and from each child series). Competitor A's places,
    // in race order, are given; competitor B fills in the other place.
    private static (Series Series, Competitor A) MakeSeries(ScoringSystem system, params int[] placesForA)
    {
        var a = new Competitor { Id = Guid.NewGuid(), Name = "A" };
        var b = new Competitor { Id = Guid.NewGuid(), Name = "B" };
        var races = new List<Race>();
        for (int i = 0; i < placesForA.Length; i++)
        {
            var firstHalf = i < placesForA.Length / 2;
            var race = new Race
            {
                Id = Guid.NewGuid(),
                Name = $"Race {i + 1}",
                Order = i + 1,
                Date = firstHalf ? Day1 : Day2,
                SourceChildSeriesId = firstHalf ? Child1 : Child2,
                Scores = new List<Score>()
            };
            race.Scores.Add(new Score { Competitor = a, CompetitorId = a.Id, Race = race, Place = placesForA[i] });
            race.Scores.Add(new Score
            {
                Competitor = b, CompetitorId = b.Id, Race = race, Place = placesForA[i] == 1 ? 2 : 1
            });
            races.Add(race);
        }
        var series = new Series
        {
            Id = Guid.NewGuid(),
            Name = "Test Series",
            Races = races,
            Competitors = new List<Competitor> { a, b },
            ScoringSystem = system
        };
        return (series, a);
    }

    private static List<string> DiscardedRaceNames(SeriesResults results, Competitor competitor)
    {
        return results.Results[competitor].CalculatedScores
            .Where(kv => kv.Value.Discard)
            .Select(kv => kv.Key.Name)
            .OrderBy(n => n)
            .ToList();
    }

    [Fact]
    public void Get_PerDay_GroupsRacesByDate()
    {
        var (series, _) = MakeSeries(MakeSystem(DiscardMethod.PerDay), 1, 1, 1, 1, 1);

        var groups = DiscardRaceGroups.Get(DiscardMethod.PerDay, series.Races);

        Assert.Equal(new[] { 2, 3 }, groups.Select(g => g.Count).OrderBy(c => c));
    }

    [Fact]
    public void Get_PerChildSeriesWithoutChildSeries_ReturnsOneGroup()
    {
        var (series, _) = MakeSeries(MakeSystem(DiscardMethod.PerChildSeries), 1, 1, 1, 1);
        foreach (var race in series.Races)
        {
            race.SourceChildSeriesId = null;
        }

        var group = Assert.Single(DiscardRaceGroups.Get(DiscardMethod.PerChildSeries, series.Races));

        Assert.Equal(4, group.Count);
    }

    [Fact]
    public void Get_PerChildSeriesWithRaceInTwoChildren_CountsRaceOnce()
    {
        var (series, _) = MakeSeries(MakeSystem(DiscardMethod.PerChildSeries), 1, 1, 1, 1);
        var shared = series.Races[0];
        series.Races.Add(new Race
        {
            Id = shared.Id, Name = shared.Name, Order = shared.Order, Date = shared.Date,
            SourceChildSeriesId = Child2
        });

        var groups = DiscardRaceGroups.Get(DiscardMethod.PerChildSeries, series.Races);

        Assert.Equal(4, groups.Sum(g => g.Count));
    }

    [Fact]
    public void CalculateResults_RaceCountPattern_DiscardsWorstInSeries()
    {
        var (series, a) = MakeSeries(MakeSystem(DiscardMethod.RaceCountPattern), 2, 2, 2, 1, 2, 2);
        series.Races[1].Scores.Single(s => s.Competitor == a).Place = null;
        series.Races[1].Scores.Single(s => s.Competitor == a).Code = "DNC";

        var results = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.AppendixA, series.ScoringSystem).CalculateResults(series);

        Assert.Equal(1, results.NumberOfDiscards);
        Assert.Equal(new[] { "Race 2" }, DiscardedRaceNames(results, a));
    }

    [Theory]
    [InlineData(DiscardMethod.PerDay)]
    [InlineData(DiscardMethod.PerChildSeries)]
    public void CalculateResults_PatternPerGroup_DiscardsWorstInEachGroup(DiscardMethod method)
    {
        // Each group of three races gets one discard: a 2 on day 1 and the 2 on day 2.
        var (series, a) = MakeSeries(MakeSystem(method), 1, 2, 1, 1, 2, 1);

        var results = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.AppendixA, series.ScoringSystem).CalculateResults(series);

        Assert.Equal(2, results.NumberOfDiscards);
        Assert.Equal(new[] { "Race 2", "Race 5" }, DiscardedRaceNames(results, a));
        Assert.Equal(method, results.DiscardMethod);
        Assert.Equal(4m, results.Results[a].TotalScore);
    }

    [Fact]
    public void CalculateResults_PerDayWithShortDay_NoDiscardOnThatDay()
    {
        // Day 1 has three races (one discard); day 2 has two (none).
        var (series, a) = MakeSeries(MakeSystem(DiscardMethod.PerDay), 1, 2, 1, 2, 2);
        series.Races[2].Date = Day1;
        series.Races[3].Date = Day2;

        var results = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.AppendixA, series.ScoringSystem).CalculateResults(series);

        Assert.Equal(1, results.NumberOfDiscards);
        Assert.Equal(new[] { "Race 2" }, DiscardedRaceNames(results, a));
    }

    [Fact]
    public void CalculateResults_LowPointAveragePerDay_UsesRacesSailedThatDay()
    {
        // A sails all of day 1 (one discard) but only two races on day 2 (none).
        var system = MakeSystem(DiscardMethod.PerDay);
        var (series, a) = MakeSeries(system, 1, 2, 1, 1, 1, 1);
        var dncRace = series.Races[5].Scores.Single(s => s.Competitor == a);
        dncRace.Place = null;
        dncRace.Code = "DNC";

        var results = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.LowPointAverageExclDnc, system).CalculateResults(series);

        Assert.Single(results.Results[a].CalculatedScores.Values, s => s.Discard);
    }

    [Fact]
    public void CalculateResults_PreserveChildSeriesDiscards_KeepsChildDiscardsOnly()
    {
        var (series, a) = MakeSeries(MakeSystem(DiscardMethod.PreserveChildSeriesDiscards), 1, 2, 1, 1, 2, 1);
        foreach (var race in series.Races)
        {
            race.CompetitorIdsDiscardedInChild = new HashSet<Guid>();
        }
        series.Races[0].CompetitorIdsDiscardedInChild.Add(a.Id);
        // A default (DNC) score the child discarded is kept discarded too.
        var missing = series.Races[3].Scores.Single(s => s.Competitor == a);
        series.Races[3].Scores.Remove(missing);
        series.Races[3].CompetitorIdsDiscardedInChild.Add(a.Id);

        var results = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.AppendixA, series.ScoringSystem).CalculateResults(series);

        Assert.Equal(new[] { "Race 1", "Race 4" }, DiscardedRaceNames(results, a));
        Assert.Equal(2, results.NumberOfDiscards);
    }

    [Fact]
    public void CalculateResults_CoxSpragueIgnoresGroups_SameAsPattern()
    {
        var (patternSeries, patternA) = MakeSeries(MakeSystem(DiscardMethod.RaceCountPattern), 1, 2, 1, 1, 2, 1);
        var (perDaySeries, perDayA) = MakeSeries(MakeSystem(DiscardMethod.PerDay), 1, 2, 1, 1, 2, 1);

        var patternResults = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.CoxSprague, patternSeries.ScoringSystem).CalculateResults(patternSeries);
        var perDayResults = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.CoxSprague, perDaySeries.ScoringSystem).CalculateResults(perDaySeries);

        Assert.Equal(patternResults.Results[patternA].TotalScore, perDayResults.Results[perDayA].TotalScore);
    }
}
