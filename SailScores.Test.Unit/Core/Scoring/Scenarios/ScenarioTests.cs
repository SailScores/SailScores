using System;
using SailScores.Api.Enumerations;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring.Scenarios;

// Tests of the scenario builder itself.
public class ScenarioTests
{
    [Fact]
    public void Score_AppendixAWithDiscard_ScoresRanksAndDiscards()
    {
        var results = Scenario.With(ScoringCalculatorType.AppendixA)
            .Discards("0,0,1")
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:2 B:1 C:3 D:DNC")
            .Race("A:1 B:3 C:2")
            .Score();

        ScenarioAssert.Ranks(results, "A", "B", "C", "D");
        ScenarioAssert.Totals(results, ("A", 2), ("B", 3), ("C", 5), ("D", 9));
        ScenarioAssert.Scores(results, "A", "1 (2) 1");
        ScenarioAssert.Scores(results, "D", "4 (DNC 5) DNC 5");
    }

    // The example in docs/Scoring-AddingACalculator.md.
    [Fact]
    public void Score_DocumentationExample_RanksAndScoresAsDocumented()
    {
        var results = Scenario.With(ScoringCalculatorType.AppendixA)
            .Discards("0")
            .Race("A:1 B:2 C:3 D:DNC")
            .NextDay()
            .Race("A:2 B:1 C:ZFP@3 D:4")
            .Score();

        // A and B tie on 3; B won the last race (A8.2). C's ZFP is 3 + 20% of DNF (5).
        ScenarioAssert.Ranks(results, "B", "A", "C", "D");
        ScenarioAssert.Scores(results, "C", "3 ZFP 4");
        ScenarioAssert.Scores(results, "D", "DNC 5 4");
    }

    [Fact]
    public void Score_CodeWithPlaceAndCodePoints_ParsesEachForm()
    {
        var results = Scenario.With(ScoringCalculatorType.AppendixA)
            .Race("A:1 B:ZFP@2 C:MAN=7.5 D:=4")
            .Score();

        Assert.Equal(2, results.Score("B", 1).RawScore.Place);
        Assert.Equal("ZFP", results.Score("B", 1).RawScore.Code);
        Assert.Equal(7.5m, results.Score("C", 1).RawScore.CodePoints);
        Assert.Equal(4m, results.Score("D", 1).RawScore.PresetScoreValue);
    }

    [Fact]
    public void Race_AfterNextDay_IsOnTheNextDayAndRestartsOrder()
    {
        var results = Scenario.With(ScoringCalculatorType.AppendixA)
            .Race("A:1")
            .Race("A:1")
            .NextDay()
            .Race("A:1")
            .Score();

        Assert.Equal(Scenario.FirstDay, results.Race(2).Date);
        Assert.Equal(2, results.Race(2).Order);
        Assert.Equal(Scenario.FirstDay.AddDays(1), results.Race(3).Date);
        Assert.Equal(1, results.Race(3).Order);
    }

    [Fact]
    public void With_UnknownSystemName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Scenario.With("No such system"));
    }

    [Fact]
    public void Race_EntryWithoutName_Throws()
    {
        Assert.Throws<FormatException>(() => Scenario.With(ScoringCalculatorType.AppendixA).Race("1"));
    }
}
