using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// Scoring works on copies of the series' scores (ScoringWorkspace.RaceScores): the series
// passed in is the same afterwards, and results carry the place and code they show.
public class InputsUnchangedTests
{
    public static TheoryData<ScoringCalculatorType> AllCalculatorTypes() =>
        new(ScoringCalculatorRegistry.All.Select(r => r.CalculatorType));

    private static List<(int? Place, string Code, decimal? CodePoints)> Inputs(Series series) =>
        series.Races.SelectMany(r => r.Scores).Select(s => (s.Place, s.Code, s.CodePoints)).ToList();

    [Theory]
    [MemberData(nameof(AllCalculatorTypes))]
    public void CalculateResults_AnySystem_LeavesSeriesScoresUnchanged(ScoringCalculatorType calculatorType)
    {
        var series = Scenario.With(calculatorType)
            .Trend(TrendOption.PreviousRace)
            .Fleet(useFullRaceScores: false, "A", "B", "C", "D")
            .Race("A:1 B:2 C:DNF@3 D:DNC E:4")
            .Race("A:2 B:1 C:3 D:4 E:5")
            .Race("A:DSQ@1 B:2 C:3 E:4")
            .BuildSeries();
        var expected = Inputs(series);
        var calculator = ScoringCalculatorRegistry.CreateCalculator(calculatorType, series.ScoringSystem);

        calculator.CalculateResults(series);
        calculator.CalculateAbsentCompetitorScore(series);

        Assert.Equal(expected, Inputs(series));
    }

    [Fact]
    public void CalculateResults_CodeThatDoesNotKeepThePlace_ShowsNoPlace()
    {
        var results = Scenario.With(ScoringCalculatorType.AppendixA)
            .Race("A:1 B:DSQ@2 C:3")
            .Score();

        Assert.Null(results.Score("B", 1).Place);
        Assert.Equal("DSQ", results.Score("B", 1).Code);
        Assert.Equal(2, results.Race(1).Scores.Single(s => s.Competitor.Name == "B").Place);
    }
}
