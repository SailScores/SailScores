using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// Score code group limits applied by a calculator (ScoreCodeGroupTests covers the model).
// RC is a club code (race committee duty) scored as the boat's average; groups limit how
// many times it can be used, and uses over the limit score as DNC. Results show the
// code entered (CalculatedScore.Code) with the DNC points, and an RC within the limit
// averages in those DNC points.
public class ScoreCodeGroupScoringTests
{
    private static readonly ScoreCode RaceCommittee = new()
    {
        Name = "RC", Formula = "AVE", Discardable = true, CameToStart = false, Started = false,
        Finished = false, PreserveResult = false, CountAsParticipation = true
    };

    private static ScoreCodeGroup Group(
        ScoreCodeGroupLimitationType type,
        decimal limit,
        ScoreCodeGroupOverageSelection selection = ScoreCodeGroupOverageSelection.LatestFirst,
        bool useNonDiscardedRaces = true,
        params string[] codes) => new()
        {
            Name = "Duty",
            LimitationType = type,
            LimitationValue = limit,
            OverageSelectionMethod = selection,
            UseNonDiscardedRaces = useNonDiscardedRaces,
            OverageCodeName = "DNC",
            IncludedCodeNames = codes.Length > 0 ? codes.ToList() : new List<string> { "RC" }
        };

    private static Scenario FourBoats(ScoreCodeGroup group, bool inherited = false) =>
        Scenario.With(ScoringCalculatorType.AppendixA)
            .Discards("0")
            .Code(RaceCommittee)
            .CodeGroup(group, inherited);

    [Fact]
    public void NumberOfRaces_LatestFirst_LaterUseScoresDnc()
    {
        var results = FourBoats(Group(ScoreCodeGroupLimitationType.NumberOfRaces, 1))
            .Race("A:2 B:1 C:3 D:4")
            .Race("A:RC B:1 C:2 D:3")
            .Race("A:RC B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "2 RC 3.5 RC 5");
        // Results show the code entered; the race was scored as the overage code.
        Assert.Equal("RC", results.Score("A", 3).Code);
        Assert.Equal("DNC", results.Score("A", 3).RawScore.Code);
        Assert.Single(results.Results.AppliedScoreCodeGroupSummaries);
    }

    [Fact]
    public void NumberOfRaces_WithinLimit_NoChangeAndNoSummary()
    {
        var results = FourBoats(Group(ScoreCodeGroupLimitationType.NumberOfRaces, 2))
            .Race("A:2 B:1 C:3 D:4")
            .Race("A:RC B:1 C:2 D:3")
            .Race("A:RC B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "2 RC 2 RC 2");
        Assert.Null(results.Results.AppliedScoreCodeGroupSummaries);
    }

    // Worst first picks the highest score at the point limits are applied: before coded
    // scores are calculated, so only codes that keep the place have a score to compare.
    [Fact]
    public void NumberOfRaces_WorstFirst_KeepsLowestPlace()
    {
        var group = Group(ScoreCodeGroupLimitationType.NumberOfRaces, 1,
            ScoreCodeGroupOverageSelection.WorstFirst, codes: "ZFP");
        var results = FourBoats(group)
            .Race("A:ZFP@1 B:2 C:3 D:4")
            .Race("A:ZFP@3 B:1 C:2 D:4")
            .Race("A:ZFP@2 B:1 C:3 D:4")
            .Score();

        ScenarioAssert.Scores(results, "A", "ZFP 2 ZFP 5 ZFP 5");
    }

    [Fact]
    public void PercentOfRaces_NonDiscardedRaces_UsesRacesLessExpectedDiscards()
    {
        // Four races, one discard: 50% of 3 rounds down to 1 use.
        var group = Group(ScoreCodeGroupLimitationType.PercentOfRaces, 50m, useNonDiscardedRaces: true);
        var results = FourBoats(group)
            .Discards("0,0,0,1")
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:RC B:1 C:2 D:3")
            .Race("A:RC B:1 C:2 D:3")
            .Race("A:1 B:2 C:3 D:4")
            .Score();

        ScenarioAssert.Scores(results, "A", "1 RC 2.3 (RC 5) 1");
    }

    [Fact]
    public void PercentOfRaces_AllRaces_UsesEveryRace()
    {
        // 50% of 4 races: 2 uses.
        var group = Group(ScoreCodeGroupLimitationType.PercentOfRaces, 50m, useNonDiscardedRaces: false);
        var results = FourBoats(group)
            .Discards("0,0,0,1")
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:RC B:1 C:2 D:3")
            .Race("A:RC B:1 C:2 D:3")
            .Race("A:1 B:2 C:3 D:4")
            .Score();

        ScenarioAssert.Scores(results, "A", "(1) RC 1 RC 1 1");
    }

    [Fact]
    public void NumberOfDates_LatestFirst_LaterDayScoresDnc()
    {
        var group = Group(ScoreCodeGroupLimitationType.NumberOfDates, 1);
        var results = FourBoats(group)
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:RC B:1 C:2 D:3")
            .NextDay()
            .Race("A:RC B:1 C:2 D:3")
            .Race("A:RC B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "1 RC 3.7 RC 5 RC 5");
    }

    [Fact]
    public void NumberOfDates_WorstFirst_DayWithFewestUsesScoresDnc()
    {
        var group = Group(ScoreCodeGroupLimitationType.NumberOfDates, 1, ScoreCodeGroupOverageSelection.WorstFirst);
        var results = FourBoats(group)
            .Race("A:1 B:2 C:3 D:4")
            .Race("A:RC B:1 C:2 D:3")
            .NextDay()
            .Race("A:RC B:1 C:2 D:3")
            .Race("A:RC B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "1 RC 5 RC 3 RC 3");
    }

    [Fact]
    public void NumberOfRaces_SeveralCodes_AreLimitedTogether()
    {
        var group = Group(ScoreCodeGroupLimitationType.NumberOfRaces, 1, codes: ["RC", "RDGAve"]);
        var results = FourBoats(group)
            .Race("A:2 B:1 C:3 D:4")
            .Race("A:RDGAve B:1 C:2 D:3")
            .Race("A:RC B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "2 RDGAve 3.5 RC 5");
    }

    // Quirk: inherited groups apply only when the system also has a group of its own.
    // See docs/Scoring-Quirks.md.
    [Fact]
    public void InheritedGroupOnly_IsNotApplied()
    {
        var results = FourBoats(Group(ScoreCodeGroupLimitationType.NumberOfRaces, 1), inherited: true)
            .Race("A:2 B:1 C:3 D:4")
            .Race("A:RC B:1 C:2 D:3")
            .Race("A:RC B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "2 RC 2 RC 2");
    }

    [Fact]
    public void InheritedGroupWithOwnGroup_IsApplied()
    {
        var results = FourBoats(Group(ScoreCodeGroupLimitationType.NumberOfRaces, 1), inherited: true)
            .CodeGroup(Group(ScoreCodeGroupLimitationType.NumberOfRaces, 5, codes: "ZFP"))
            .Race("A:2 B:1 C:3 D:4")
            .Race("A:RC B:1 C:2 D:3")
            .Race("A:RC B:1 C:2 D:3")
            .Score();

        ScenarioAssert.Scores(results, "A", "2 RC 3.5 RC 5");
    }
}
