using System.Collections.Generic;
using SailScores.Api.Enumerations;
using SailScores.Core.Scoring;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring.Tiebreaks;

// The ranking chain and the standard rankings. Each tiebreak is also covered by the
// calculator, scenario and characterization tests.
public class RankingTests
{
    private sealed class Fixed(int result) : ITieBreak
    {
        public int Calls { get; private set; }

        public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y)
        {
            Calls++;
            return result;
        }
    }

    private static SeriesCompetitorResults Total(decimal? total) => new()
    {
        TotalScore = total,
        CalculatedScores = new Dictionary<SailScores.Core.Model.Race, CalculatedScore>()
    };

    [Fact]
    public void Compare_FirstStepSeparates_LaterStepsNotUsed()
    {
        var later = new Fixed(1);
        var ranking = Ranking.By(new Fixed(-1)).Then(later);

        Assert.Equal(-1, ranking.Compare(null, Total(1), Total(2)));
        Assert.Equal(0, later.Calls);
    }

    [Fact]
    public void Compare_EveryStepTies_ReturnsZero()
    {
        Assert.Equal(0, Ranking.By(new Fixed(0)).Then(new Fixed(0)).Compare(null, Total(1), Total(1)));
    }

    [Fact]
    public void Then_ReturnsNewRanking_OriginalUnchanged()
    {
        var first = Ranking.By(new Fixed(0));

        var longer = first.Then(new Fixed(1));

        Assert.Single(first.Steps);
        Assert.Equal(2, longer.Steps.Count);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, -1)]
    public void LowestTotal_ZeroAgainstFive_RanksZeroLastOnlyWhenAsked(bool zeroTotalRanksLast, int expected)
    {
        Assert.Equal(expected, new LowestTotal(zeroTotalRanksLast).Compare(null, Total(0), Total(5)));
    }

    [Fact]
    public void LowestTotal_NoTotal_RanksLast()
    {
        Assert.Equal(1, new LowestTotal(zeroTotalRanksLast: false).Compare(null, Total(null), Total(50)));
    }

    [Fact]
    public void HighestTotal_NoTotal_RanksLast()
    {
        Assert.Equal(1, new HighestTotal().Compare(null, Total(null), Total(0)));
    }

    [Fact]
    public void StandardRankings_ListTheirSteps()
    {
        Assert.Equal("LowestTotal, then AppendixA81LowPoint, then AppendixA82", Rankings.LowPoint.ToString());
        Assert.Equal("HighestTotal, then MoreRacesSailed, then AppendixA81HighPoint, then AppendixA82",
            Rankings.HighPoint.ToString());
        Assert.Equal("LowestTotal, then PwaBeatenMoreOften, then PwaBetterScoresIncludingDiscards, "
            + "then PwaBetterDiscard, then AppendixA82", Rankings.Pwa.ToString());
    }

    // Quirk: A8.1 leaves out scores of 0, so under First = 0 a win doesn't count in the
    // tiebreak (docs/Scoring-Quirks.md). A and B tie on 4. By RRS A8.1 A ranks ahead (0, 4
    // against 2, 2); without the 0, A's 4 loses to B's 2.
    [Fact]
    public void AppendixA81_FirstIsZero_LeavesOutWins()
    {
        var results = Scenario.With(ScoringCalculatorType.AppAAltFirstIsZero)
            .Discards("0")
            .Race("C:1 B:2 D:3 A:4 E:5")
            .Race("A:1 B:2 C:3 D:4 E:5")
            .Score();

        ScenarioAssert.Totals(results, ("A", 4), ("B", 4));
        ScenarioAssert.Ranks(results, "C", "B", "A", "D", "E");
    }
}
