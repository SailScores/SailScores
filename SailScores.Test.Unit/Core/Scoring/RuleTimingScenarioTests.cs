using System.Collections.Generic;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// Quirk: Wawasee 2026 works out which days count for each competitor in turn, after earlier
// competitors' score code group overages have changed their codes (docs/Scoring-Quirks.md).
//
// Day 2 has four starters, the minimum, but X's breakdown (BKD, a start) is its second, over
// the limit of one, so it becomes DNC (not a start). Before X is scored, day 2 has four
// starters; after, three.
public class RuleTimingScenarioTests
{
    private static ScenarioResults Score(params string[] competitorOrder) =>
        Scenario.With(ScoringCalculatorType.Wawasee2026)
            .Settings(s => s.PenaltyScoreValue = 8m)
            .Participation(0)
            .CodeGroup(new ScoreCodeGroup
            {
                Name = "Breakdowns",
                LimitationType = ScoreCodeGroupLimitationType.NumberOfRaces,
                LimitationValue = 1,
                OverageCodeName = "DNC",
                IncludedCodeNames = new List<string> { "BKD" }
            })
            .Competitors(competitorOrder)
            .Race("A:1 B:2 C:3 D:4 X:BKD")
            .NextDay()
            .Race("A:1 B:2 C:3 X:BKD")
            .Score();

    [Fact]
    public void Wawasee_OverageScoredFirst_DayTwoCountsForNobody()
    {
        var results = Score("X", "A", "B", "C", "D");

        ScenarioAssert.Scores(results, "A", "1 (1)");
        ScenarioAssert.Totals(results, ("A", 1));
    }

    [Fact]
    public void Wawasee_OverageScoredLast_DayTwoNotDiscardedButLeftOutOfTotal()
    {
        var results = Score("A", "B", "C", "D", "X");

        // Day 2 counted when A was scored, so it isn't a discard; the totals use the races
        // left by the last boat scored (X), so A's total leaves it out anyway.
        ScenarioAssert.Scores(results, "A", "1 1");
        ScenarioAssert.Totals(results, ("A", 1));
    }
}
