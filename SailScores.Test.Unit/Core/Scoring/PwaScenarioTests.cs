using SailScores.Api.Enumerations;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// PWA Standard tiebreaks (Rankings.Pwa), as ranked today. PWA scores a win 0.7.
//
// Quirk (bug): tiebreak (C), the better discarded result, compares a boat's discards with
// its own, so it never breaks a tie. See docs/Scoring-Quirks.md.
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
}
