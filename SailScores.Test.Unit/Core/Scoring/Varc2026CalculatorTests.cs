using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

public class Varc2026CalculatorTests
{
    private static readonly DateTime Day = new(2026, 6, 6);

    private static ScoringSystem MakeSystem(
        ScoringCalculatorType type,
        ScoringSystemSettings settings) => new()
    {
        Id = Guid.NewGuid(),
        Name = type.ToString(),
        CalculatorType = type,
        DiscardPattern = "0",
        EffectiveDiscardMethod = DiscardMethod.RaceCountPattern,
        EffectiveSettings = settings,
        ScoreCodes = new List<ScoreCode>
        {
            new()
            {
                Name = "DNC", Formula = "FIX", FormulaValue = 0m, CameToStart = false,
                Started = false, Finished = false, Discardable = true
            },
            new()
            {
                Name = "DNF", Formula = "FIN+", FormulaValue = 1m, CameToStart = true,
                Started = true, Finished = false, Discardable = true
            }
        },
        InheritedScoreCodes = new List<ScoreCode>()
    };

    // results[r][c] is competitor c's result in race r: a place, a score code, or null
    // for no score.
    private static (Series Series, List<Competitor> Competitors) MakeSeries(
        ScoringSystem system,
        params string[][] results)
    {
        var competitors = results[0]
            .Select((_, i) => new Competitor { Id = Guid.NewGuid(), Name = $"C{i}" })
            .ToList();
        var races = new List<Race>();
        for (int r = 0; r < results.Length; r++)
        {
            var race = new Race
            {
                Id = Guid.NewGuid(), Name = $"Race {r + 1}", Order = r + 1,
                Date = Day, State = RaceState.Raced, Scores = new List<Score>()
            };
            for (int c = 0; c < competitors.Count; c++)
            {
                var result = results[r][c];
                if (result == null)
                {
                    continue;
                }
                var isPlace = int.TryParse(result, out var place);
                race.Scores.Add(new Score
                {
                    Competitor = competitors[c], CompetitorId = competitors[c].Id, Race = race,
                    Place = isPlace ? place : null,
                    Code = isPlace ? null : result
                });
            }
            races.Add(race);
        }
        var series = new Series
        {
            Id = Guid.NewGuid(), Name = "Test", Races = races,
            Competitors = competitors, ScoringSystem = system
        };
        return (series, competitors);
    }

    private static (SeriesResults Results, List<Competitor> Competitors) Score(
        ScoringCalculatorType type,
        ScoringSystemSettings settings,
        params string[][] results)
    {
        var system = MakeSystem(type, settings);
        var (series, competitors) = MakeSeries(system, results);
        return (ScoringCalculatorRegistry.CreateCalculator(type, system).CalculateResults(series), competitors);
    }

    private static (SeriesResults Results, List<Competitor> Competitors) ScoreShorthanded(
        int scoresToCount,
        params string[][] results) =>
        Score(ScoringCalculatorType.Varc2026Shorthanded,
            new ScoringSystemSettings { ScoresToCount = scoresToCount }, results);

    private static decimal? ScoreValue(SeriesResults results, Competitor competitor, int raceIndex) =>
        results.Results[competitor].CalculatedScores
            .Single(kv => kv.Key.Order == raceIndex + 1).Value.ScoreValue;

    [Fact]
    public void CreateCalculator_Varc2026Shorthanded_RequiresScoresToCount()
    {
        var system = MakeSystem(ScoringCalculatorType.Varc2026Shorthanded, new ScoringSystemSettings());

        Assert.Equal("VARC 2026", Assert.IsType<ScoringPipeline>(
            ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.Varc2026Shorthanded, system)).Recipe.Name);
        var errors = ScoringCalculatorRegistry.GetValidationErrors(new ScoringSystem(), system).ToList();
        var error = Assert.Single(errors);
        Assert.Equal(nameof(ScoringSystemSettings.ScoresToCount), error.PropertyName);
    }

    [Fact]
    public void CalculateResults_BestThreeOfFour_TotalsBestThreeAndDiscardsTheRest()
    {
        var (results, c) = ScoreShorthanded(3,
            new[] { "1", "2", "3", "4", "5", "6" },
            new[] { "2", "1", "3", "4", "5", "6" },
            new[] { "3", "2", "1", "4", "5", "6" },
            new[] { "6", "5", "4", "3", "2", "1" });

        // C0: 100, 99, 98, 95; C1: 99, 100, 99, 96; C2: 98, 98, 100, 97.
        var c0 = results.Results[c[0]];
        Assert.Equal(297m, c0.TotalScore);
        Assert.Equal(392m, c0.TotalScoreIncludingDiscards);
        Assert.Equal(new[] { "Race 4" }, c0.CalculatedScores.Where(kv => kv.Value.Discard).Select(kv => kv.Key.Name));
        Assert.Equal(298m, results.Results[c[1]].TotalScore);
        Assert.Equal(1, results.Results[c[1]].Rank);
        Assert.Equal(2, c0.Rank);
        Assert.Equal(3, results.Results[c[2]].Rank);
        Assert.Equal(1, results.NumberOfDiscards);
        Assert.False(results.LowerScoreWins);
    }

    [Fact]
    public void CalculateResults_ThreeBoatsRacing_ReducesScoresByTwo()
    {
        var (results, c) = ScoreShorthanded(3, new[] { "1", "2", "3" });

        // (100 - 5) + 3 = 98 for first, and so on.
        Assert.Equal(98m, ScoreValue(results, c[0], 0));
        Assert.Equal(97m, ScoreValue(results, c[1], 0));
        Assert.Equal(96m, ScoreValue(results, c[2], 0));
    }

    [Fact]
    public void CalculateResults_DnfAndDnc_ScoreHundredLessBoatsRacingAndZero()
    {
        var (results, c) = ScoreShorthanded(3,
            new[] { "1", "2", "3", "4", "5", "DNF", "DNC", null });

        // Six boats racing (the DNF started): DNF scores 100 - 6.
        Assert.Equal(94m, ScoreValue(results, c[5], 0));
        Assert.Equal(0m, ScoreValue(results, c[6], 0));
        Assert.Equal(0m, ScoreValue(results, c[7], 0));
    }

    // Best two of three. C0 counts 100 + 98, C1 counts 99 + 99: A8.1 breaks the tie
    // on C0's first.
    private static readonly string[][] A81Tie =
    {
        new[] { "1", "2", "3", "4", "5" },
        new[] { "5", "2", "1", "3", "4" },
        new[] { "3", "2", "1", "4", "5" }
    };

    [Fact]
    public void CalculateResults_TiedTotals_BrokenByBestCountedScores()
    {
        var (results, c) = ScoreShorthanded(2, A81Tie);

        Assert.Equal(198m, results.Results[c[0]].TotalScore);
        Assert.Equal(198m, results.Results[c[1]].TotalScore);
        Assert.Equal(2, results.Results[c[0]].Rank);
        Assert.Equal(3, results.Results[c[1]].Rank);
    }

    // Best two of three. C0 and C1 both count 100 + 99; C0 beat C1 in the last race.
    private static readonly string[][] A82Tie =
    {
        new[] { "1", "2", "3", "4", "5" },
        new[] { "2", "1", "3", "4", "5" },
        new[] { "3", "4", "1", "2", "5" }
    };

    [Fact]
    public void CalculateResults_TiedAfterA81_BrokenByLastRace()
    {
        var (results, c) = ScoreShorthanded(2, A82Tie);

        Assert.Equal(199m, results.Results[c[0]].TotalScore);
        Assert.Equal(199m, results.Results[c[1]].TotalScore);
        Assert.Equal(1, results.Results[c[0]].Rank);
        Assert.Equal(2, results.Results[c[1]].Rank);
    }
    private static (SeriesResults Results, List<Competitor> Competitors) ScoreVarc2026(
        ScoringSystemSettings settings,
        params string[][] results) =>
        Score(ScoringCalculatorType.Varc2026, settings, results);

    [Fact]
    public void Registration_Varc2026_HasVarcFieldsAndNoErrorsForYesNoSetting()
    {
        var registration = ScoringCalculatorRegistry.Get(ScoringCalculatorType.Varc2026);
        var parent = MakeSystem(ScoringCalculatorType.Varc2026,
            new ScoringSystemSettings { ScoresToCount = 3, MinimumCompetitors = 2 });
        var child = new ScoringSystem { Settings = new ScoringSystemSettings { TiesStandAfterA81 = true } };

        Assert.Equal(
            new[]
            {
                nameof(ScoringSystemSettings.ScoresToCount),
                nameof(ScoringSystemSettings.MinimumCompetitors),
                nameof(ScoringSystemSettings.TiesStandAfterA81)
            },
            registration.Fields.Select(f => f.PropertyName));
        Assert.Empty(ScoringCalculatorRegistry.GetValidationErrors(child, parent));
        Assert.DoesNotContain(
            ScoringCalculatorRegistry.Get(ScoringCalculatorType.Varc2026Shorthanded).Fields,
            f => f.PropertyName == nameof(ScoringSystemSettings.MinimumCompetitors));
    }

    [Fact]
    public void ToJson_TiesStandAfterA81_RoundTrips()
    {
        var json = ScoringSystemSettings.ToJson(new ScoringSystemSettings { TiesStandAfterA81 = false });

        Assert.False(ScoringSystemSettings.FromJson(json).TiesStandAfterA81);
    }

    [Fact]
    public void CalculateResults_RaceWithOneBoat_DoesNotCount()
    {
        // Race 2: only C0 came to the start. Best 3 of the counted races.
        var (results, c) = ScoreVarc2026(
            new ScoringSystemSettings { ScoresToCount = 3, MinimumCompetitors = 2 },
            new[] { "2", "1" },
            new[] { "1", "DNC" },
            new[] { "2", "1" });

        var c0 = results.Results[c[0]];
        // Two boats racing: 101 - 3 - place.
        Assert.Equal(96m, ScoreValue(results, c[0], 1));
        Assert.True(c0.CalculatedScores.Single(kv => kv.Key.Order == 2).Value.Discard);
        Assert.Equal(192m, c0.TotalScore);
        Assert.Equal(194m, results.Results[c[1]].TotalScore);
        Assert.Equal(1, results.NumberOfDiscards);
    }

    [Fact]
    public void CalculateResults_RaceWithTwoBoats_Counts()
    {
        var (results, c) = ScoreVarc2026(
            new ScoringSystemSettings { ScoresToCount = 3, MinimumCompetitors = 2 },
            new[] { "1", "2" });

        Assert.Equal(97m, results.Results[c[0]].TotalScore);
        Assert.Equal(0, results.NumberOfDiscards);
    }

    [Fact]
    public void CalculateResults_TiesStandAfterA81_TiedBoatsShareRank()
    {
        var (results, c) = ScoreVarc2026(
            new ScoringSystemSettings { ScoresToCount = 2, TiesStandAfterA81 = true },
            A82Tie);

        Assert.Equal(1, results.Results[c[0]].Rank);
        Assert.Equal(1, results.Results[c[1]].Rank);
        Assert.Equal(3, results.Results[c[2]].Rank);
    }

    [Fact]
    public void CalculateResults_A82_SkipsRacesThatDidNotCount()
    {
        // A82Tie plus a last race only C1 came to the start for: it doesn't count, so
        // A8.2 still uses race 3, which C0 won against C1.
        var races = A82Tie.Append(new[] { null, "1", null, null, null }).ToArray();

        var (results, c) = ScoreVarc2026(
            new ScoringSystemSettings { ScoresToCount = 2, MinimumCompetitors = 2 },
            races);

        Assert.Equal(1, results.Results[c[0]].Rank);
        Assert.Equal(2, results.Results[c[1]].Rank);
    }
}
