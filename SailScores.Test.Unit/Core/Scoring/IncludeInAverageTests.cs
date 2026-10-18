using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

public class IncludeInAverageTests
{
    private const string Incl = "Incl";
    private const string Excl = "Excl";

    // Two competitors, three races. Competitor A places 1, 1, then gets the given code.
    // Competitor B places 2, 2, 1. DNC scores 3 (series competitors + 1);
    // DNF scores 2 (finishers + 1).
    private static (Series Series, Competitor A) MakeSeries(string codeName, bool? includeInAverage)
    {
        var system = new ScoringSystem
        {
            Id = Guid.NewGuid(),
            Name = "Low Point Average",
            DiscardPattern = "0",
            InheritedScoreCodes = new List<ScoreCode>()
        };
        system.ScoreCodes = new List<ScoreCode>
        {
            new()
            {
                Name = "DNC", Formula = "SER+", FormulaValue = 1m, CameToStart = false,
                Started = false, Finished = false, Discardable = true, ScoringSystemId = system.Id
            },
            new()
            {
                Name = "DNF", Formula = "FIN+", FormulaValue = 1m, CameToStart = true,
                Started = true, Finished = false, Discardable = true, ScoringSystemId = system.Id
            }
        };
        system.ScoreCodes.Single(c => c.Name == codeName).IncludeInAverage = includeInAverage;

        var a = new Competitor { Id = Guid.NewGuid(), Name = "A" };
        var b = new Competitor { Id = Guid.NewGuid(), Name = "B" };
        var races = new List<Race>();
        for (int i = 0; i < 3; i++)
        {
            var race = new Race
            {
                Id = Guid.NewGuid(),
                Name = $"Race {i + 1}",
                Order = i + 1,
                Date = new DateTime(2026, 6, 1).AddDays(i),
                Scores = new List<Score>()
            };
            var lastRace = i == 2;
            race.Scores.Add(new Score
            {
                Competitor = a, Race = race,
                Place = lastRace ? null : 1,
                Code = lastRace ? codeName : null
            });
            race.Scores.Add(new Score { Competitor = b, Race = race, Place = lastRace ? 1 : 2 });
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

    private static decimal? GetTotal(string calculatorName, string codeName, bool? includeInAverage)
    {
        var (series, a) = MakeSeries(codeName, includeInAverage);
        IScoringCalculator calculator = calculatorName == Incl
            ? ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.LowPointAverageInclDnc, series.ScoringSystem)
            : ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.LowPointAverageExclDnc, series.ScoringSystem);
        return calculator.CalculateResults(series).Results[a].TotalScore;
    }

    [Theory]
    // Null keeps each calculator's current rule.
    [InlineData(Incl, "DNC", null, 5, 3)]
    [InlineData(Excl, "DNC", null, 2, 2)]
    [InlineData(Incl, "DNF", null, 4, 3)]
    [InlineData(Excl, "DNF", null, 4, 3)]
    // True includes a score the calculator would leave out.
    [InlineData(Excl, "DNC", true, 5, 3)]
    [InlineData(Incl, "DNC", true, 5, 3)]
    // False leaves out a score the calculator would include.
    [InlineData(Incl, "DNC", false, 2, 2)]
    [InlineData(Incl, "DNF", false, 2, 2)]
    [InlineData(Excl, "DNF", false, 2, 2)]
    [InlineData(Excl, "DNC", false, 2, 2)]
    public void CalculateResults_IncludeInAverage_ControlsWhetherCodeCounts(
        string calculatorName,
        string codeName,
        bool? includeInAverage,
        int expectedPoints,
        int expectedRaces)
    {
        var total = GetTotal(calculatorName, codeName, includeInAverage);

        Assert.Equal((decimal)expectedPoints / expectedRaces, total);
    }
}
