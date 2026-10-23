using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// Score code formulas, as scored today. Several pin quirks listed in docs/Scoring-Quirks.md.
public class FormulaScenarioTests
{
    private static ScoreCode Code(string name, string formula, decimal? value = null, bool preserveResult = false) => new()
    {
        Name = name, Description = name, Formula = formula, FormulaValue = value,
        PreserveResult = preserveResult, Discardable = true, CameToStart = true, Started = true,
        Finished = false, CountAsParticipation = true
    };

    private static Scenario AppendixA() =>
        Scenario.With(ScoringCalculatorType.AppendixA).Discards("0");

    [Fact]
    public void Man_WithCodePoints_ScoresCodePoints()
    {
        var results = AppendixA().Race("A:RDG=2.5 B:1 C:2 D:3").Score();

        ScenarioAssert.Scores(results, "A", "RDG 2.5");
    }

    // Quirk: MAN without code points scores the place.
    [Fact]
    public void Man_WithoutCodePoints_ScoresPlace()
    {
        var results = AppendixA().Race("A:RDG@3 B:1 C:2 D:4").Score();

        ScenarioAssert.Scores(results, "A", "RDG 3");
    }

    [Fact]
    public void Man_WithoutCodePointsOrPlace_ScoresNothing()
    {
        var results = AppendixA().Race("A:RDG B:1 C:2 D:3").Score();

        ScenarioAssert.Scores(results, "A", "RDG null");
    }

    [Fact]
    public void AverageWhole_RoundsHalfUp()
    {
        var results = AppendixA()
            .Code(Code("AW", "AVE W"))
            .Code(Code("AV", "AVE"))
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:2 B:1 C:3 D:4")
            .Race("A:AW B:1 C:2 D:3")
            .Race("A:AV B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "1 2 AW 2 AV 1.5");
    }

    [Fact]
    public void AverageWholePrior_UsesOnlyEarlierRaces()
    {
        var results = AppendixA()
            .Code(Code("AWP", "AVE W P"))
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:2 B:1 C:3 D:4")
            .Race("A:AWP B:1 C:2 D:3")
            .Race("A:4 B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "1 2 AWP 2 4");
    }

    [Fact]
    public void AverageWholeNonDiscarded_LeavesOutTheDiscardCount()
    {
        var results = AppendixA()
            .Discards("0,0,0,1")
            .Code(Code("AWN", "AVE W ND"))
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:4 B:1 C:2 D:3")
            .Race("A:2 B:1 C:3 D:4")
            .Race("A:AWN B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "1 (4) 2 AWN 2");
    }

    // Quirk: AVE ND averages the best (races − averages − expected discards) scores, not the
    // races left after the discards actually chosen. Here BFD can't be discarded, so the 2nd is
    // discarded, but the average is of the 1st and the 2nd.
    [Fact]
    public void AverageNonDiscarded_AveragesBestScoresNotUndiscardedOnes()
    {
        var results = AppendixA()
            .Discards("0,0,0,1")
            .Code(Code("AND", "AVE ND"))
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:BFD B:1 C:2 D:3")
            .Race("A:2 B:1 C:3 D:4")
            .Race("A:AND B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "1 BFD 5 (2) AND 1.5");
    }

    [Fact]
    public void Average_FirstIsZero_CountsWins()
    {
        var results = Scenario.With(ScoringCalculatorType.AppAAltFirstIsZero)
            .Discards("0")
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:3 B:1 C:2 D:4")
            .Race("A:RDGAve B:1 C:2 D:3")
            .Score();

        // First = 0 scores a win 0 and other places as the place: the average of 0 and 3.
        ScenarioAssert.Scores(results, "A", "1:0 3 RDGAve 1.5");
    }

    [Fact]
    public void Fix_UpperCase_ScoresFormulaValue()
    {
        var results = AppendixA().Code(Code("XF", "FIX", 3m)).Race("A:XF B:1 C:2 D:3").Score();

        ScenarioAssert.Scores(results, "A", "XF 3");
    }

    [Fact]
    public void Fix_LowerCase_ScoresFormulaValue()
    {
        var results = AppendixA().Code(Code("XF", "fix", 3m)).Race("A:XF B:1 C:2 D:3").Score();

        ScenarioAssert.Scores(results, "A", "XF 3");
    }

    [Fact]
    public void Man_LowerCase_ScoresCodePoints()
    {
        var results = AppendixA().Code(Code("XM", "man")).Race("A:XM=2.5 B:1 C:2 D:3").Score();

        ScenarioAssert.Scores(results, "A", "XM 2.5");
    }

    [Theory]
    [InlineData("TIE")]
    [InlineData("tie")]
    public void Tie_AnyCase_TiesWithPreviousFinisher(string formula)
    {
        var tie = Code("XT", formula, preserveResult: true);
        tie.Finished = true;
        tie.AdjustOtherScores = false;
        var results = AppendixA().Code(tie).Race("A:1 B:2 C:XT@3 D:4").Score();

        ScenarioAssert.Scores(results, "C", "XT 2.5");
    }

    private static ScoreCode Cod(string name, string scoreLike)
    {
        var cod = Code(name, "COD");
        cod.ScoreLike = scoreLike;
        return cod;
    }

    [Fact]
    public void Cod_ScoreLikeDnf_ScoresDnfScore()
    {
        var results = AppendixA().Code(Cod("CD", "DNF")).Race("A:CD@2 B:1 C:3 D:4").Score();

        // DNF in Appendix A for regattas: boats in the series + 1.
        ScenarioAssert.Scores(results, "A", "CD 5");
        Assert.Null(results.Score("A", 1).RawScore.Place);
    }

    [Fact]
    public void Cod_ScoreLikeFixedCode_ScoresItsFormulaValue()
    {
        var results = AppendixA().Code(Code("XF", "FIX", 3m)).Code(Cod("CD", "XF"))
            .Race("A:CD B:1 C:2 D:4").Score();

        ScenarioAssert.Scores(results, "A", "CD 3");
    }

    [Fact]
    public void Cod_ScoreLikeAnotherCod_FollowsTheChain()
    {
        var results = AppendixA().Code(Code("XF", "FIX", 3m)).Code(Cod("C1", "C2")).Code(Cod("C2", "XF"))
            .Race("A:C1 B:1 C:2 D:4").Score();

        ScenarioAssert.Scores(results, "A", "C1 3");
    }

    [Fact]
    public void Cod_ScoreLikeAverage_ScoresTheAverage()
    {
        var results = AppendixA().Code(Cod("CD", "RDGAve"))
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:3 B:1 C:2 D:4")
            .Race("A:CD B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "1 3 CD 2");
    }

    // Low point average without DNCs leaves codes scored like DNC out of the average, as it
    // does DNC itself.
    [Theory]
    [InlineData("DNC")]
    [InlineData("CD")]
    public void Cod_ScoreLikeDnc_LeftOutOfLowPointAverageWithoutDncs(string code)
    {
        var cod = Cod("CD", "DNC");
        cod.CameToStart = false;
        cod.Started = false;
        var results = Scenario.With(ScoringCalculatorType.LowPointAverageExclDnc).Discards("0").Participation(0)
            .Code(cod)
            .Race("A:1 B:2 C:3")
            .Race($"A:{code} B:1 C:2")
            .Race("A:3 B:1 C:2")
            .Score();

        ScenarioAssert.Totals(results, ("A", 2));
    }

    // A loop, or no ScoreLike, gives no formula: the code keeps its place-based score (its
    // place is cleared, so no boat is ahead of it and it scores 1).
    [Theory]
    [InlineData(null)]
    [InlineData("C2")]
    public void Cod_NoScoreLikeOrLoop_KeepsPlaceBasedScore(string scoreLike)
    {
        var results = AppendixA().Code(Cod("C1", scoreLike)).Code(Cod("C2", "C1"))
            .Race("A:C1@2 B:1 C:3 D:4").Score();

        ScenarioAssert.Scores(results, "A", "C1 1");
    }

    [Theory]
    [InlineData("COD", "Scored like DNF")]
    [InlineData("AVE W", "Average of results rounded to a whole number")]
    [InlineData("AVE W ND", "Average of non-discarded results rounded to a whole number")]
    [InlineData("AVE W P", "Average of results in prior races rounded to a whole number")]
    [InlineData("MAN", "Manually entered value")]
    public void CodesUsed_Formula_DescribesFormula(string formula, string expected)
    {
        var code = Code("XX", formula);
        code.ScoreLike = "DNF";
        var results = AppendixA().Code(code).Race("A:XX=2 B:1 C:2 D:3").Score();

        Assert.StartsWith(expected, results.Results.ScoreCodesUsed["XX"].Formula);
    }

    // Rule 5.3 series systems score DNF as boats that came to the start + 1, so the PLC%
    // penalty depends on how many came to the start.
    [Fact]
    public void PlacePlusPercent_DnfCameToStartPlusOne_PenaltyFromBoatsAtStart()
    {
        var results = Scenario.With("Appendix A For Series - Rule 5.3")
            .Discards("0")
            .Race("A:ZFP@2 B:1 C:3 D:4")
            .Race("A:ZFP@2 B:1 C:3 D:DNC")
            .Score();

        // DNF is 5, then 4: 20% is 1, then 0.8.
        ScenarioAssert.Scores(results, "A", "ZFP 3 ZFP 2.8");
    }

    // RRS 44.3(c): the penalty is a percentage of the DNF score, here series-based (SER+, boats
    // in the series + 1), so a boat missing the race doesn't lower it.
    [Fact]
    public void PlacePlusPercent_DnfSeriesCompetitorsPlusOne_PenaltyFromDnfScore()
    {
        var results = AppendixA()
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:2 B:1 C:ZFP@3")
            .Score();

        // DNF scores 5 (four boats in the series + 1); the penalty is 20% of 5.
        ScenarioAssert.Scores(results, "C", "3 ZFP 4");
        ScenarioAssert.Scores(results, "D", "4 DNC 5");
    }

    [Fact]
    public void PlacePlusPercent_FixedDnfScore_PenaltyFromFixedValue()
    {
        var results = AppendixA()
            .Code(Code("DNF", "FIX", 10m))
            .Race("A:ZFP@2 B:1 C:3 D:4")
            .Score();

        ScenarioAssert.Scores(results, "A", "ZFP 4");
    }

    [Theory]
    [InlineData(ScoringCalculatorType.AppAAltFirstIsPoint75, "1:1.375")]
    [InlineData(ScoringCalculatorType.AppAAltFirstIsPoint7, "1:1.35")]
    [InlineData(ScoringCalculatorType.AppAAltFirstIsZero, "1")]
    [InlineData(ScoringCalculatorType.AppendixA, "1:1.5")]
    public void TieForFirst_AveragesFirstPlaceValueWithSecond(ScoringCalculatorType calculator, string expected)
    {
        var results = Scenario.With(calculator).Discards("0").Race("A:1 B:1 C:3").Score();

        ScenarioAssert.Scores(results, "A", expected);
    }
}
