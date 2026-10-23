using System;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// PWA Standard tiebreaks (Rankings.Pwa). PWA scores a win 0.7.
public class PwaScenarioTests
{
    [Fact]
    public void Tie_BeatenEachOtherEquallyOften_MoreLowScoresWins()
    {
        // A and B both total 14 and each beat the other twice. A has two 2nds.
        var results = Scenario.With(ScoringCalculatorType.PwaStandard)
            .Discards("0")
            .Race("E:1 A:2 B:3 C:4 D:5")
            .Race("E:1 A:2 B:3 C:4 D:5")
            .Race("E:1 C:2 D:3 B:4 A:5")
            .Race("E:1 C:2 D:3 B:4 A:5")
            .Score();

        ScenarioAssert.Totals(results, ("A", 14), ("B", 14));
        ScenarioAssert.Ranks(results, "E", "C", "A", "B", "D");
    }

    [Fact]
    public void Tie_SameScoresIncludingDiscards_BetterLastRaceWins()
    {
        // With one discard A and B total 5.7, each beat the other twice and have the same scores.
        var results = Scenario.With(ScoringCalculatorType.PwaStandard)
            .Discards("0,0,0,1")
            .Race("A:1 B:2 C:3 D:4")
            .Race("C:1 D:2 B:3 A:4")
            .Race("B:1 A:2 C:3 D:4")
            .Race("C:1 D:2 A:3 B:4")
            .Score();

        ScenarioAssert.Totals(results, ("A", 5.7m), ("B", 5.7m));
        ScenarioAssert.Ranks(results, "C", "A", "B", "D");
    }

    private static SeriesCompetitorResults Discarding(params decimal[] discarded) => new()
    {
        CalculatedScores = discarded
            .Select(s => new CalculatedScore { ScoreValue = s, Discard = true })
            .Append(new CalculatedScore { ScoreValue = 1, Discard = false })
            .ToDictionary(_ => new Race { Id = Guid.NewGuid() }, s => s)
    };

    // Tiebreak (C) on its own: in a series it's reached only when two boats have the same
    // scores, discards included, and the same total, which takes non-discardable codes.
    [Fact]
    public void BetterDiscard_BetterDiscardedScore_RanksAhead()
    {
        // Best first: 2, 6 against 3, 5.
        Assert.Equal(-1, new PwaBetterDiscard().Compare(null, Discarding(2, 6), Discarding(3, 5)));
        Assert.Equal(1, new PwaBetterDiscard().Compare(null, Discarding(3, 5), Discarding(2, 6)));
    }

    [Fact]
    public void BetterDiscard_SameAsFarAsShorterList_MoreDiscardsRanksAhead()
    {
        Assert.Equal(-1, new PwaBetterDiscard().Compare(null, Discarding(3, 6), Discarding(3)));
        Assert.Equal(1, new PwaBetterDiscard().Compare(null, Discarding(3), Discarding(3, 6)));
    }

    [Fact]
    public void BetterDiscard_SameDiscards_Ties()
    {
        Assert.Equal(0, new PwaBetterDiscard().Compare(null, Discarding(3, 5), Discarding(5, 3)));
    }
}
