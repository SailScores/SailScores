using SailScores.Api.Enumerations;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// Series scored for one fleet (Series.FleetId set), with and without UseFullRaceScores, and
// other club-system branches the coverage pass found unexercised.
public class FleetScenarioTests
{
    // Day minimum of four boats: five boats raced, but only A, B and C are in the fleet.
    [Theory]
    [InlineData(true, "1 1")]
    [InlineData(false, "(1) (1)")]
    public void Wawasee_FleetSeries_DayMinimumCountsFleetBoatsUnlessFullRaceScores(bool useFullRaceScores, string expected)
    {
        var results = Scenario.With(ScoringCalculatorType.Wawasee2026)
            .Settings(s => s.PenaltyScoreValue = 8m)
            .Participation(0)
            .Fleet(useFullRaceScores, "A", "B", "C")
            .Race("A:1 X:2 B:3 Y:4 C:5")
            .Race("A:1 X:2 B:3 Y:4 C:5")
            .Score();

        ScenarioAssert.Scores(results, "A", expected);
    }

    [Fact]
    public void AppendixA_FleetSeries_RecalculatesPlacesWithinFleet()
    {
        var results = Scenario.With(ScoringCalculatorType.AppendixA)
            .Discards("0")
            .Fleet(false, "A", "B", "C")
            .Race("A:1 X:2 B:3 Y:4 C:5 Z:DNF")
            .Score();

        ScenarioAssert.Scores(results, "B", "3:2");
        ScenarioAssert.Scores(results, "C", "5:3");
    }

    // VARC 2026: a race needs two boats that came to the start. With fewer than five
    // boats, Top X scores a win 100 − (5 − boats racing): full race 3 and 2 boats, fleet 2 and 1.
    [Theory]
    [InlineData(true, "1:98 1:97")]
    [InlineData(false, "1:97 (1:96)")]
    public void Varc_FleetSeries_MinimumBoatsCountsFleetBoatsUnlessFullRaceScores(bool useFullRaceScores, string expected)
    {
        var results = Scenario.With(ScoringCalculatorType.Varc2026)
            .Fleet(useFullRaceScores, "A", "B")
            .Race("A:1 B:2 X:3")
            .Race("A:1 X:2")
            .Score();

        ScenarioAssert.Scores(results, "A", expected);
    }

    [Fact]
    public void Varc_NoScoresToCount_EveryScoreCounts()
    {
        var results = Scenario.With(ScoringCalculatorType.Varc2026Shorthanded)
            .Settings(s => s.ScoresToCount = null)
            .Race("A:1 B:2").Race("A:2 B:1").Race("A:1 B:2").Race("A:2 B:1")
            .Score();

        Assert.DoesNotContain("(", results.Scores("A"));
    }

    [Fact]
    public void Varc_PresetScore_IsKept()
    {
        var results = Scenario.With(ScoringCalculatorType.Varc2026Shorthanded)
            .Race("A:1 B:=50")
            .Score();

        ScenarioAssert.Scores(results, "B", "50");
    }

    [Fact]
    public void CoxSprague_OneStarter_ScoresZero()
    {
        var results = Scenario.With(ScoringCalculatorType.CoxSprague)
            .Race("A:1 B:DNC")
            .Score();

        ScenarioAssert.Scores(results, "A", "1:0");
    }

    // With full race scores B keeps its whole-race place, 3.
    [Theory]
    [InlineData(ScoringCalculatorType.AppendixA, "3")]
    [InlineData(ScoringCalculatorType.LowPointAverageInclDnc, "3")]
    [InlineData(ScoringCalculatorType.LowPointAverageExclDnc, "3")]
    [InlineData(ScoringCalculatorType.Pbg, "3")]
    public void FleetWithFullRaceScores_KeepsWholeRacePlace(ScoringCalculatorType calculator, string expected)
    {
        var results = Scenario.With(calculator)
            .Discards("0")
            .Fleet(useFullRaceScores: true, "A", "B")
            .Race("A:1 X:2 B:3")
            .Score();

        ScenarioAssert.Scores(results, "B", expected);
    }

    [Theory]
    [InlineData(ScoringCalculatorType.LowPointAverageInclDnc)]
    [InlineData(ScoringCalculatorType.LowPointAverageExclDnc)]
    [InlineData(ScoringCalculatorType.Pbg)]
    public void LowPointAverage_FleetSeries_RecalculatesPlacesWithinFleet(ScoringCalculatorType calculator)
    {
        var results = Scenario.With(calculator)
            .Discards("0")
            .Fleet(false, "A", "B")
            .Race("A:1 X:2 B:3")
            .Score();

        ScenarioAssert.Scores(results, "B", "3:2");
    }
}
