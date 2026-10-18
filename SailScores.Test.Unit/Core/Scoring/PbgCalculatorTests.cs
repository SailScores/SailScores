using SailScores.Api.Enumerations;
using SailScores.Core.Scoring;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// The shared PBG base system: low point average without DNCs, 50% to rank counting only
// races not discarded, and child series discards kept in a summary series.
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
        Assert.Equal(100m, results.For("C0").ParticipationPercent);
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
        // Three kept races sailed of the three not discarded.
        Assert.Equal(100m, results.For("C0").ParticipationPercent);
    }

    [Fact]
    public void Pbg_SummaryParticipationLeavesOutDiscardedRaces()
    {
        var results = PbgSummary("0").Score();

        // Three kept races sailed of the seven not discarded: under 50%, so unranked,
        // although four of eight races sailed would have been enough.
        var c1 = results.For("C1");
        Assert.Equal(300m / 7m, c1.ParticipationPercent);
        Assert.Null(c1.TotalScore);
        Assert.Equal(2m, c1.Average);
    }
}
