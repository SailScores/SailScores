using SailScores.Api.Enumerations;
using SailScores.Core.Scoring;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// The shared PBG base system (docs/ScoringSystems/Pbg.md): low point average without DNCs,
// 50% to rank after discards, and child series discards kept in a summary series.
public class PbgCalculatorTests
{
    private static Scenario Pbg(string discards) =>
        Scenario.With(ScoringCalculatorType.Pbg).Discards(discards);

    // Two daily series of four races each, scored as one summary keeping each day's discards.
    //   C0 sails all 8; day 1 discarded race 4.
    //   C1 sails day 1 only; day 1 discarded race 1.
    private static Scenario PbgSummary(string discards) =>
        Pbg(discards)
            .Race("C0:1 C1:2").DiscardedInChild("C1")
            .Race("C0:1 C1:2")
            .Race("C0:1 C1:2")
            .Race("C0:1 C1:2").DiscardedInChild("C0")
            .NextDay()
            .Race("C0:1").Race("C0:1").Race("C0:1").Race("C0:1");

    [Fact]
    public void CreateCalculator_Pbg_UsesPbgCalculatorWithNetColumnsAndScoreHeader()
    {
        Assert.Equal("PBG", Assert.IsType<ScoringPipeline>(
            ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.Pbg, Pbg("0").System)).Recipe.Name);
        var registration = ScoringCalculatorRegistry.Get(ScoringCalculatorType.Pbg);
        Assert.Equal(
            new[] { ScoringColumns.NetPointsKey, ScoringColumns.NetRacesKey },
            registration.Columns.Select(c => c.Key));
        Assert.Equal("Score", registration.TotalHeader);
        Assert.Equal("Total", ScoringCalculatorRegistry.Get(ScoringCalculatorType.AppendixA).TotalHeader);
    }

    // Average systems leave out the pre-discard total, but PBG keeps it (beside Net Points).
    [Fact]
    public void GetColumns_Pbg_KeepsPreDiscardTotalUnlikeOtherAverageSystems()
    {
        var pbg = ScoringCalculatorRegistry.GetColumns(ScoringCalculatorType.Pbg)
            .Single(c => c.Key == ScoringColumns.PreDiscardTotalKey);
        var average = ScoringCalculatorRegistry.GetColumns(ScoringCalculatorType.LowPointAverageInclDnc)
            .Single(c => c.Key == ScoringColumns.PreDiscardTotalKey);

        Assert.True(pbg.IncludeForPercentSystems);
        Assert.False(average.IncludeForPercentSystems);
    }

    [Fact]
    public void Resolve_PbgColumns_UseAverageNumeratorAndDenominator()
    {
        var results = Pbg("0,0,0,1")
            .Race("C0:1 C1:2").Race("C0:1 C1:2").Race("C0:1 C1:2").Race("C0:1 C1:2")
            .Score();
        var c1 = results.For("C1");
        var scores = new List<SailScores.Core.FlatModel.FlatSeriesScore>
        {
            new() { CompetitorId = results.Competitor("C1").Id, PointsEarned = c1.PointsEarned, PointsPossible = c1.PointsPossible }
        };

        var columns = ScoringColumns.Resolve(
            ScoringCalculatorRegistry.GetColumns(ScoringCalculatorType.Pbg), null, true, scores);

        // Four 2nds, one discarded: 6 points over 3 races.
        Assert.Contains(columns, c => c.Key == ScoringColumns.NetPointsKey && c.Visibility == ColumnVisibility.Always);
        Assert.Contains(columns, c => c.Key == ScoringColumns.NetRacesKey && c.Visibility == ColumnVisibility.Always);
        Assert.Equal(6m, scores[0].ColumnValues[ScoringColumns.NetPointsKey].Value);
        Assert.Equal(3m, scores[0].ColumnValues[ScoringColumns.NetRacesKey].Value);
        ScenarioAssert.Totals(results, ("C1", 2));
    }

    [Fact]
    public void Pbg_SummaryWithChildDiscards_KeepsChildDiscardsAndIgnoresPattern()
    {
        // The pattern would discard 2 of 8 races.
        var results = PbgSummary("0,1,2").Score();

        Assert.Equal(DiscardMethod.PreserveChildSeriesDiscards, results.Results.DiscardMethod);
        ScenarioAssert.Scores(results, "C0", "1 1 1 (1) 1 1 1 1");
    }

    [Fact]
    public void Pbg_NonSummarySeries_UsesDiscardPattern()
    {
        // Four races: C0 sails all (1sts), C1 sails 1-3 (2nds). Pattern: one discard at 4 races.
        var results = Pbg("0,0,0,1")
            .Race("C0:1 C1:2").Race("C0:1 C1:2").Race("C0:1 C1:2").Race("C0:1")
            .Score();

        Assert.Equal(DiscardMethod.RaceCountPattern, results.Results.DiscardMethod);
        Assert.Equal(1, results.Results.NumberOfDiscards);
        Assert.Equal(1, results.For("C0").CalculatedScores.Values.Count(s => s.Discard));
        ScenarioAssert.Totals(results, ("C0", 1));
        // Three races not discarded, of the four less the pattern's one discard.
        Assert.Equal(100m, results.For("C0").ParticipationPercent);
    }

    [Fact]
    public void Pbg_SummaryParticipation_IsAfterTheMaximumDiscards()
    {
        // Three days of 8 races; each day discards one race for a boat that sailed 6 or more.
        // A boat sailing all 24 discards 3, so boats are measured against 21 races.
        var sails = new Dictionary<string, int[]>
        {
            ["A"] = [8, 8, 8], // 21 of 21
            ["B"] = [8, 8, 0], // 16 less 2 discards: 14 of 21
            ["C"] = [8, 0, 0], // 7 of 21
            ["D"] = [6, 6, 0], // 12 less 2 discards: 10 of 21
            ["E"] = [4, 4, 4], // 12, no discards: 12 of 21
        };
        var scenario = Pbg("0,0,0,0,0,1");
        for (int day = 0; day < 3; day++)
        {
            if (day > 0)
            {
                scenario.NextDay();
            }
            for (int race = 0; race < 8; race++)
            {
                var boats = sails.Keys.Where(b => race < sails[b][day]).ToList();
                scenario.Race(string.Join(" ", boats.Select((b, i) => $"{b}:{i + 1}")));
                var discarded = boats.Where(b => sails[b][day] >= 6 && race == sails[b][day] - 1).ToArray();
                if (discarded.Length > 0)
                {
                    scenario.DiscardedInChild(discarded);
                }
            }
        }

        var results = scenario.Score();

        Assert.Equal(100m, results.For("A").ParticipationPercent);
        Assert.Equal(14m * 100m / 21m, results.For("B").ParticipationPercent);
        Assert.Equal(7m * 100m / 21m, results.For("C").ParticipationPercent);
        Assert.Null(results.For("C").TotalScore);
        Assert.Equal(10m * 100m / 21m, results.For("D").ParticipationPercent);
        Assert.Null(results.For("D").TotalScore);
        Assert.Equal(12m * 100m / 21m, results.For("E").ParticipationPercent);
        Assert.NotNull(results.For("E").TotalScore);
        // 50% of the 21 races, rounded up.
        Assert.Equal(11, results.Results.RacesRequired);
    }

    [Fact]
    public void Pbg_DailySeries_SixRacesSailedDiscardsWorst()
    {
        // The base system's pattern: one discard for a boat that sailed 6 races or more.
        // Ten races, so the maximum discards is one: nine to sail. C1 sails 6 (worst a 3), five
        // after its discard; C2 sails 5, with no discard: both five of nine.
        var results = Pbg("0,0,0,0,0,1")
            .Race("C0:1 C2:2 C1:3")
            .Race("C0:1 C1:2 C2:3").Race("C0:1 C1:2 C2:3").Race("C0:1 C1:2 C2:3").Race("C0:1 C1:2 C2:3")
            .Race("C0:1 C1:2")
            .Race("C0:1").Race("C0:1").Race("C0:1").Race("C0:1")
            .Score();

        var c1 = results.For("C1");
        Assert.Equal(1, c1.CalculatedScores.Values.Count(s => s.Discard));
        Assert.Equal(5m * 100m / 9m, c1.ParticipationPercent);
        var c2 = results.For("C2");
        Assert.Equal(0, c2.CalculatedScores.Values.Count(s => s.Discard));
        Assert.Equal(5m * 100m / 9m, c2.ParticipationPercent);
        ScenarioAssert.Totals(results, ("C0", 1), ("C1", 2), ("C2", 2.8m));
    }

    [Fact]
    public void Pbg_DailySeries_HalfTheRacesWithADiscard_IsShortAfterDiscards()
    {
        // Twelve races, the maximum discards one: C1 sails 6 and discards one, five of eleven, so
        // isn't ranked.
        var results = Pbg("0,0,0,0,0,1")
            .Race("C0:1 C1:3")
            .Race("C0:1 C1:2").Race("C0:1 C1:2").Race("C0:1 C1:2").Race("C0:1 C1:2")
            .Race("C0:1 C1:2")
            .Race("C0:1").Race("C0:1").Race("C0:1").Race("C0:1").Race("C0:1").Race("C0:1")
            .Score();

        var c1 = results.For("C1");
        Assert.Equal(5m * 100m / 11m, c1.ParticipationPercent);
        Assert.Null(c1.TotalScore);
    }

    [Fact]
    public void Pbg_DailySeries_NonExcludableWorstScore_DiscardsNextWorst()
    {
        // C1 sails 6 races, the worst a DNE: the DNE stays and the 3 is discarded.
        var results = Pbg("0,0,0,0,0,1")
            .Race("C0:1 C2:2 C1:3")
            .Race("C0:1 C1:2 C2:3").Race("C0:1 C1:2 C2:3").Race("C0:1 C1:2 C2:3").Race("C0:1 C1:2 C2:3")
            .Race("C0:1 C2:2 C1:DNE")
            .Score();

        var discarded = results.For("C1").CalculatedScores.Values.Single(s => s.Discard);
        Assert.Equal(3m, discarded.ScoreValue);
        Assert.False(results.For("C1").CalculatedScores.Values.Single(s => s.RawScore.Code == "DNE").Discard);
    }
}
