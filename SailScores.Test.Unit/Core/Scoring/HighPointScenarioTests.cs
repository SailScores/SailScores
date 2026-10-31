using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// Cox-Sprague and Top X high point branches, as scored today.
//
// Quirk: both score every coded result that came to the start (except TIE) as one place
// behind the last starter, so PLC% codes such as ZFP don't use the place or a penalty. Their
// GetPenaltyScore overrides are never reached. See docs/Scoring-Quirks.md.
public class HighPointScenarioTests
{
    [Fact]
    public void CoxSprague_PlacePlusPercent_ScoresOneBehindLast()
    {
        var results = Scenario.With(ScoringCalculatorType.CoxSprague)
            .Race("A:ZFP@1 B:2 C:3 D:4 E:5")
            .Score();

        // Five starters: 52, 46, 42, 38, 35, and 32 for a sixth place.
        ScenarioAssert.Scores(results, "A", "ZFP 32");
        ScenarioAssert.Scores(results, "B", "2:46");
    }

    [Fact]
    public void CoxSprague_Discard_DropsRaceThatLowersPercentMost()
    {
        var results = Scenario.With(ScoringCalculatorType.CoxSprague)
            .Discards("0,0,1")
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:4 B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "1:43 1:43 (4:29)");
        ScenarioAssert.Scores(results, "B", "(2:37) 2:37 1:43");
        Assert.Equal(100m, results.For("A").TotalScore);
    }

    [Fact]
    public void CoxSprague_NoRaceAtStart_NothingToDiscard()
    {
        var results = Scenario.With(ScoringCalculatorType.CoxSprague)
            .Discards("0,1")
            .Race("A:DNS B:1 C:2")
            .Race("A:DNS B:1 C:2")
            .Score();

        ScenarioAssert.Scores(results, "A", "DNS 0 DNS 0");
    }

    // Issue 413: averages are left unscored.
    [Fact]
    public void CoxSprague_Average_ScoresNothing()
    {
        var results = Scenario.With(ScoringCalculatorType.CoxSprague)
            .Race("A:1 B:2 C:3")
            .Race("A:RDGAve B:1 C:2")
            .Score();

        ScenarioAssert.Scores(results, "A", "1:31 RDGAve null");
    }

    [Fact]
    public void TopX_PlacePlusPercent_ScoresOneBehindLast()
    {
        var results = Scenario.With(ScoringCalculatorType.TopXHighPoint)
            .Race("A:ZFP@1 B:2 C:3 D:4 E:5")
            .Race("A:ZFP@5 B:1 C:2 D:3 E:4")
            .Score();

        ScenarioAssert.Scores(results, "A", "ZFP 95 ZFP 95");
        ScenarioAssert.Scores(results, "E", "5:96 4:97");
    }

    [Fact]
    public void CoxSprague_NonDiscardableCodeAtStart_IsKept()
    {
        var results = Scenario.With(ScoringCalculatorType.CoxSprague)
            .Discards("0,1")
            .Code(new ScoreCode
            {
                Name = "NDQ", Formula = "FIX", FormulaValue = 0, CameToStart = true, Started = true,
                Finished = false, Discardable = false, PreserveResult = false
            })
            .Race("A:1 B:2 C:3")
            .Race("A:NDQ B:1 C:2")
            .Score();

        // NDQ came to the start, so scores one behind last (17 of 3 starters). It can't be
        // discarded, and dropping the win would lower A's percentage, so both count.
        ScenarioAssert.Scores(results, "A", "1:31 NDQ 17");
    }

    [Fact]
    public void CoxSprague_NonDiscardableCodeAtStart_NextWorstRaceDiscarded()
    {
        var results = Scenario.With(ScoringCalculatorType.CoxSprague)
            .Discards("0,0,1")
            .Code(new ScoreCode
            {
                Name = "NDQ", Formula = "FIX", FormulaValue = 0, CameToStart = true, Started = true,
                Finished = false, Discardable = false, PreserveResult = false
            })
            .Race("A:1 B:2 C:3")
            .Race("A:NDQ B:1 C:2")
            .Race("C:1 B:2 A:3")
            .Score();

        // NDQ (17) is A's worst score but can't be discarded, so the 3rd (21) goes instead.
        ScenarioAssert.Scores(results, "A", "1:31 NDQ 17 (3:21)");
    }
}
