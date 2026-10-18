using System;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// The shared Appendix B Windsurfing base system: Appendix A as changed by RRS Appendix B8.
// Each test pins a row of the clause map in docs/ScoringSystems/AppendixB-Windsurfing.md;
// the worked examples there are the WorkedExample tests.
public class AppendixBWindsurfingCalculatorTests
{
    private static Scenario AppendixB() => Scenario.With(ScoringCalculatorType.AppendixBWindsurfing);

    private static int DiscardCount(ScenarioResults results, string name) =>
        results.For(name).CalculatedScores.Values.Count(s => s.Discard);

    [Fact]
    public void Registration_AppendixBWindsurfing_HasNoFieldsOrColumnsAndPatternDiscards()
    {
        var registration = ScoringCalculatorRegistry.Get(ScoringCalculatorType.AppendixBWindsurfing);

        Assert.Equal("Appendix B Windsurfing",
            Assert.IsType<ScoringPipeline>(registration.Create(AppendixB().System)).Recipe.Name);
        Assert.Empty(registration.Fields);
        Assert.Empty(registration.Columns);
        Assert.Equal(ScoringCalculatorRegistry.PatternDiscardMethods, registration.AllowedDiscardMethods);
        Assert.Equal(ScoringCalculatorRegistry.DefaultTotalHeader, registration.TotalHeader);
    }

    [Fact]
    public void BaseSystem_AppendixBWindsurfing_ScoresA52WithoutZfp()
    {
        var system = AppendixB().System;

        Assert.Equal("0,0,0,0,1,1,1,1,1,1,1,2", system.DiscardPattern);
        Assert.DoesNotContain(system.ScoreCodes, c => c.Name == "ZFP");
        Assert.All(
            system.ScoreCodes.Where(c => c.Name is "DNC" or "DNS" or "OCS" or "UFD" or "BFD"
                or "DNF" or "RET" or "DSQ" or "DNE" or "NSC"),
            c => Assert.Equal("SER+", c.Formula));
        Assert.Equal(
            new[] { "BFD", "DNE" },
            system.ScoreCodes.Where(c => c.Discardable == false).Select(c => c.Name).Order());
    }

    // B8 A2.1: the worst score is excluded from 5 to 11 races, the two worst from 12.
    [Theory]
    [InlineData(4, 0)]
    [InlineData(5, 1)]
    [InlineData(11, 1)]
    [InlineData(12, 2)]
    [InlineData(15, 2)]
    public void CalculateResults_RaceCount_ExcludesByAppendixBSchedule(int raceCount, int expectedDiscards)
    {
        var scenario = AppendixB();
        for (int i = 0; i < raceCount; i++)
        {
            scenario.Race(i % 2 == 0 ? "A:1 B:2 C:3" : "B:1 C:2 A:3");
        }

        var results = scenario.Score();

        Assert.Equal(expectedDiscards, DiscardCount(results, "A"));
        Assert.Equal(expectedDiscards, DiscardCount(results, "B"));
    }

    // Rule 90.3(b): DNE can't be excluded, so the next worst score is.
    [Fact]
    public void CalculateResults_DneIsWorstScore_NextWorstDiscarded()
    {
        var results = AppendixB()
            .Race("B:1 C:2 D:3 E:4 A:DNE")
            .Race("A:1 B:2 C:3 D:4 E:5")
            .Race("A:1 C:2 B:3 D:4 E:5")
            .Race("A:1 B:2 C:3 D:4 E:5")
            .Race("B:1 A:2 C:3 D:4 E:5")
            .Score();

        ScenarioAssert.Scores(results, "A", "DNE 6 1 1 1 (2)");
        ScenarioAssert.Totals(results, ("A", 9));
    }

    // B8 A2.1: the NOR or SIs may make a different arrangement, on the club's child system.
    [Fact]
    public void CalculateResults_ChildSystemDiscardPattern_Overrides()
    {
        var results = AppendixB()
            .Discards("0")
            .Race("A:1 B:2").Race("A:1 B:2").Race("A:1 B:2").Race("A:1 B:2").Race("B:1 A:2")
            .Score();

        Assert.Equal(0, DiscardCount(results, "A"));
        ScenarioAssert.Totals(results, ("A", 6), ("B", 9));
    }

    // A club applies A5.3 by overriding codes with CTS+ on its child system. Here the base
    // codes are inherited and DNF is the child's own.
    [Fact]
    public void CalculateResults_ChildSystemCtsCodes_ScoresA53()
    {
        var scenario = AppendixB();
        var system = scenario.System;
        var baseDnf = system.ScoreCodes.Single(c => c.Name == "DNF");
        system.InheritedScoreCodes = system.ScoreCodes.ToList();
        system.ScoreCodes =
        [
            new ScoreCode
            {
                Id = Guid.NewGuid(),
                Name = "DNF",
                Formula = "CTS+",
                FormulaValue = 1,
                CameToStart = baseDnf.CameToStart,
                Started = baseDnf.Started,
                Finished = baseDnf.Finished,
                Discardable = baseDnf.Discardable,
                PreserveResult = baseDnf.PreserveResult,
                AdjustOtherScores = baseDnf.AdjustOtherScores
            }
        ];

        // Four boats came to the start; five are in the series.
        var results = scenario.Race("A:1 B:2 C:3 D:DNF E:DNC").Score();

        ScenarioAssert.Scores(results, "D", "DNF 5");
        ScenarioAssert.Scores(results, "E", "DNC 6");
    }

    [Fact]
    public void CalculateResults_BaseCodes_ScoreA52()
    {
        var results = AppendixB().Race("A:1 B:2 C:3 D:DNF E:DNC").Score();

        ScenarioAssert.Scores(results, "D", "DNF 6");
        ScenarioAssert.Scores(results, "E", "DNC 6");
    }

    private static Scenario WorkedExample1(Scenario scenario) =>
        scenario
            .Race("B:1 A:2 C:3 D:4 E:5")
            .Race("B:1 A:2 D:3 E:4 C:5")
            .Race("C:1 A:2 B:3 E:4 D:5")
            .Race("C:1 D:2 A:3 B:4 E:5")
            .Race("D:1 E:2 C:3 A:4 B:5");

    // Worked example 1: A and B tie on 9; A's excluded 4 beats B's excluded 5 (B8 A8.1).
    [Fact]
    public void WorkedExample1_TieOnTotal_BetterExcludedScoreRanksAhead()
    {
        var results = WorkedExample1(AppendixB()).Score();

        ScenarioAssert.Totals(results, ("C", 8), ("A", 9), ("B", 9), ("D", 10), ("E", 15));
        ScenarioAssert.Scores(results, "A", "2 2 2 3 (4)");
        ScenarioAssert.Scores(results, "B", "1 1 3 4 (5)");
        ScenarioAssert.Ranks(results, "C", "A", "B", "D", "E");
    }

    // Worked example 1: E scores 5 in races 1 and 4; the earlier is excluded (B8 A2.1).
    [Fact]
    public void WorkedExample1_EqualWorstScores_EarliestRaceDiscarded()
    {
        var results = WorkedExample1(AppendixB()).Score();

        ScenarioAssert.Scores(results, "E", "(5) 4 4 5 2");
    }

    // The same series under Appendix A: its A8.1 uses the counted scores, so B ranks ahead.
    [Fact]
    public void WorkedExample1_UnderAppendixA_CountedScoresDecideTheOtherWay()
    {
        var results = WorkedExample1(Scenario.With("Appendix A For Regatta").Discards("0,0,0,0,1")).Score();

        ScenarioAssert.Ranks(results, "C", "B", "A", "D", "E");
    }

    // Worked example 2: A and B tie on 10 and both excluded a 5; by all scores, A's 1 beats
    // B's 2 (B8 A8.2).
    [Fact]
    public void WorkedExample2_SameExcludedScores_BetterScoresIncludingExcludedRankAhead()
    {
        var results = AppendixB()
            .Race("A:1 C:2 B:3 D:4 E:5")
            .Race("C:1 B:2 A:3 D:4 E:5")
            .Race("D:1 B:2 A:3 C:4 E:5")
            .Race("E:1 C:2 B:3 D:4 A:5")
            .Race("E:1 D:2 A:3 C:4 B:5")
            .Score();

        ScenarioAssert.Totals(results, ("C", 9), ("A", 10), ("B", 10), ("D", 11), ("E", 12));
        ScenarioAssert.Scores(results, "C", "2 1 (4) 2 4");
        ScenarioAssert.Scores(results, "D", "(4) 4 1 4 2");
        ScenarioAssert.Ranks(results, "C", "A", "B", "D", "E");
    }

    // Worked example 3: four races, so nothing is excluded and A8.1 can't decide; the tied
    // boats have the same scores, so the last race decides (B8 A8.3).
    [Fact]
    public void WorkedExample3_NoExcludedScoresAndSameScores_LastRaceDecides()
    {
        var results = AppendixB()
            .Race("A:1 B:2 C:3 D:4")
            .Race("B:1 A:2 C:3 D:4")
            .Race("A:1 B:2 D:3 C:4")
            .Race("B:1 A:2 D:3 C:4")
            .Score();

        ScenarioAssert.Totals(results, ("B", 6), ("A", 6), ("D", 14), ("C", 14));
        ScenarioAssert.Ranks(results, "B", "A", "D", "C");
    }

    // B8 A8.3 uses excluded scores: A's last race is its excluded 5, B's is a 3.
    [Fact]
    public void Tie_LastRaceExcluded_StillDecides()
    {
        var results = AppendixB()
            .Race("A:1 B:2 C:3 D:4 E:5")
            .Race("B:1 A:2 D:3 E:4 C:5")
            .Race("C:1 E:2 A:3 B:4 D:5")
            .Race("E:1 C:2 D:3 A:4 B:5")
            .Race("C:1 D:2 B:3 E:4 A:5")
            .Score();

        ScenarioAssert.Scores(results, "A", "1 2 3 4 (5)");
        ScenarioAssert.Scores(results, "B", "2 1 4 (5) 3");
        ScenarioAssert.Ranks(results, "C", "B", "A", "E", "D");
    }

    private static SeriesCompetitorResults Excluding(params decimal[] excluded) => new()
    {
        CalculatedScores = excluded
            .Select(s => new CalculatedScore { ScoreValue = s, Discard = true })
            .Append(new CalculatedScore { ScoreValue = 1, Discard = false })
            .ToDictionary(_ => new Race { Id = Guid.NewGuid() }, s => s)
    };

    // B8 A8.1 with different numbers of excluded scores: compare as far as the shorter list
    // goes (decision 1 in the doc).
    [Fact]
    public void ExcludedScores_SameAsFarAsShorterList_Ties()
    {
        Assert.Equal(0, new AppendixBExcludedScores().Compare(null, Excluding(3), Excluding(3, 6)));
    }

    [Fact]
    public void ExcludedScores_DifferenceWithinShorterList_BetterRanksAhead()
    {
        // Best first: 2 against 1, 3.
        Assert.Equal(1, new AppendixBExcludedScores().Compare(null, Excluding(2), Excluding(3, 1)));
        Assert.Equal(-1, new AppendixBExcludedScores().Compare(null, Excluding(3, 1), Excluding(2)));
    }

    [Fact]
    public void ExcludedScores_NoneExcluded_Ties()
    {
        Assert.Equal(0, new AppendixBExcludedScores().Compare(null, Excluding(), Excluding()));
    }
}
