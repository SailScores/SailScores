using SailScores.Api.Enumerations;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// Discard patterns and trend, as scored today.
public class SeriesOptionScenarioTests
{
    private static Scenario AppendixA(string discards) =>
        Scenario.With(ScoringCalculatorType.AppendixA).Discards(discards);

    // An entry that isn't a number uses the entry for one race fewer.
    [Theory]
    [InlineData("0,x", 0)]
    [InlineData("0,1,x", 1)]
    [InlineData("x", 0)]
    public void DiscardPattern_EntryNotANumber_UsesPreviousEntry(string pattern, int expectedDiscards)
    {
        var scenario = AppendixA(pattern);
        for (var i = 0; i < pattern.Split(',').Length; i++)
        {
            scenario.Race("A:1 B:2");
        }

        var results = scenario.Score();

        Assert.Equal(expectedDiscards, results.Results.NumberOfDiscards);
    }

    [Fact]
    public void DiscardPattern_MoreRacesThanEntries_UsesLastEntry()
    {
        var results = AppendixA("0,1")
            .Race("A:1 B:2").Race("A:1 B:2").Race("A:1 B:2").Race("A:2 B:1")
            .Score();

        Assert.Equal(1, results.Results.NumberOfDiscards);
    }

    [Fact]
    public void Trend_PreviousRace_ComparesWithRankBeforeLastRace()
    {
        var results = AppendixA("0")
            .Trend(TrendOption.PreviousRace)
            .Race("A:1 B:2 C:3")
            .Race("A:3 B:1 C:2")
            .Score();

        ScenarioAssert.Ranks(results, "B", "A", "C");
        Assert.Equal(1, results.For("B").Trend);
        Assert.Equal(-1, results.For("A").Trend);
        Assert.Equal(0, results.For("C").Trend);
    }

    [Fact]
    public void Trend_PreviousRaceWhenLastRaceAbandoned_ComparesWithRankBeforeLastRaceSailed()
    {
        var results = AppendixA("0")
            .Trend(TrendOption.PreviousRace)
            .Race("A:1 B:2 C:3")
            .Race("A:3 B:1 C:2")
            .Race("", RaceState.Abandoned)
            .Score();

        Assert.Equal(1, results.For("B").Trend);
    }

    [Fact]
    public void Trend_OnlyAbandonedRacesBefore_NoTrend()
    {
        var results = AppendixA("0")
            .Trend(TrendOption.PreviousRace)
            .Race("", RaceState.Abandoned)
            .Race("A:1 B:2")
            .Score();

        Assert.Null(results.For("A").Trend);
    }

    [Fact]
    public void Trend_EveryRaceAbandoned_NoTrend()
    {
        var results = AppendixA("0")
            .Trend(TrendOption.PreviousRace)
            .Competitors("A", "B")
            .Race("", RaceState.Abandoned)
            .Race("", RaceState.Abandoned)
            .Score();

        Assert.Null(results.For("A").Trend);
    }

    [Fact]
    public void Trend_PreviousDay_ComparesWithRankBeforeLastDay()
    {
        var results = AppendixA("0")
            .Trend(TrendOption.PreviousDay)
            .Race("A:1 B:2 C:3")
            .NextDay()
            .Race("A:3 B:1 C:2")
            .Race("A:3 B:1 C:2")
            .Score();

        // A and C tie on 7; A wins on A8.1 (it has the best score). A was 1st after day one.
        ScenarioAssert.Ranks(results, "B", "A", "C");
        Assert.Equal(-1, results.For("A").Trend);
    }

    [Fact]
    public void Trend_BoatFirstSailingInLastRace_ComparedWithItsDncRank()
    {
        var results = AppendixA("0")
            .Trend(TrendOption.PreviousRace)
            .Race("A:1 B:2")
            .Race("A:2 B:3 C:1")
            .Score();

        // C is a series competitor, so it scored DNC in race 1 and was 3rd before race 2.
        // B and C tie on 5; C wins on A8.1.
        ScenarioAssert.Ranks(results, "A", "C", "B");
        Assert.Equal(1, results.For("C").Trend);
    }

    // Quirk: Appendix A pre-2025 has no participation requirement (docs/Scoring-Quirks.md).
    [Theory]
    [InlineData(ScoringCalculatorType.AppendixA, null)]
    [InlineData(ScoringCalculatorType.AppendixAPre2025, 2)]
    public void Participation_BoatSailedOneOfThree_RankedOnlyBeforeThe2025Rules(
        ScoringCalculatorType calculator, int? expectedRank)
    {
        var results = Scenario.With(calculator)
            .Discards("0")
            .Participation(50)
            .Race("A:1 B:2")
            .Race("A:1")
            .Race("A:1")
            .Score();

        ScenarioAssert.Rank(results, "B", expectedRank);
    }
}
