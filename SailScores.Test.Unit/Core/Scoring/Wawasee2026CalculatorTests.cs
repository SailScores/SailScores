using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Test.Unit.Core.Scoring.Scenarios;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

// The shared Wawasee 2026 base system: 51% to rank, 70% to avoid penalties, four boats a day
// for races to count, and DNC, EXC and BKD scored as the boat's average finish.
public class Wawasee2026CalculatorTests
{
    // Each fleet's child system sets its penalty points.
    private static Scenario Wawasee(decimal penaltyPoints = 8m) =>
        Scenario.With(ScoringCalculatorType.Wawasee2026)
            .Settings(s => s.PenaltyScoreValue = penaltyPoints);

    // Ten races, two a day. C0, C3, C4 and C5 sail every race.
    //   C1 sails races 1-6 (all 2nds) and misses 7-10.
    //   C2 sails races 1-4 (all 3rds), is excused for 5-6 and misses 7-10.
    private static ScenarioResults ScoreSeason() =>
        Wawasee()
            .Race("C0:1 C1:2 C2:3 C3:4 C4:5 C5:6").Race("C0:1 C1:2 C2:3 C3:4 C4:5 C5:6").NextDay()
            .Race("C0:1 C1:2 C2:3 C3:4 C4:5 C5:6").Race("C0:1 C1:2 C2:3 C3:4 C4:5 C5:6").NextDay()
            .Race("C0:1 C1:2 C2:EXC C3:3 C4:4 C5:5").Race("C0:1 C1:2 C2:EXC C3:3 C4:4 C5:5").NextDay()
            .Race("C0:1 C3:2 C4:3 C5:4").Race("C0:1 C3:2 C4:3 C5:4").NextDay()
            .Race("C0:1 C3:2 C4:3 C5:4").Race("C0:1 C3:2 C4:3 C5:4")
            .Score();

    [Fact]
    public void Registration_Wawasee2026_HasPenaltyFieldsColumnsAndPointsHeader()
    {
        var registration = ScoringCalculatorRegistry.Get(ScoringCalculatorType.Wawasee2026);

        Assert.Equal("Wawasee 2026", Assert.IsType<ScoringPipeline>(registration.Create(Wawasee().System)).Recipe.Name);
        Assert.Equal(
            new[]
            {
                nameof(ScoringSystemSettings.PenaltyThresholdPercent),
                nameof(ScoringSystemSettings.PenaltyScoreValue),
                nameof(ScoringSystemSettings.MinimumCompetitors)
            },
            registration.Fields.Select(f => f.PropertyName));
        Assert.Equal(
            new[]
            {
                ScoringColumns.PenaltiesKey,
                ScoringColumns.AverageFinishKey,
                ScoringColumns.AverageWithPenaltiesKey
            },
            registration.Columns.Select(c => c.Key));
        Assert.Equal("Points (with Penalties)", registration.TotalHeader);
    }

    [Fact]
    public void AverageWithPenaltiesColumn_PointsOverCountedRaces_DividesTotalWithPenalties()
    {
        // Race points 8 plus one penalty of 8, over 2 counted races.
        var score = new SailScores.Core.FlatModel.FlatSeriesScore { PointsEarned = 16m, PointsPossible = 2m };

        Assert.Equal(8m, ScoringColumns.AverageWithPenalties.ValueFromScore(score).Value);
        Assert.Null(ScoringColumns.AverageWithPenalties.ValueFromScore(
            new SailScores.Core.FlatModel.FlatSeriesScore { PointsEarned = 0m, PointsPossible = 0m }));
    }

    [Fact]
    public void CalculateResults_MissedRaces_ScoreAverageFinishAndPenaltyBelowThreshold()
    {
        var results = ScoreSeason();

        // Six 2nds, four misses at the average of 2, and one race short of 7: 12 + 8 + 8.
        var c1 = results.For("C1");
        Assert.Equal(2m, c1.Average);
        ScenarioAssert.Scores(results, "C1", "2 2 2 2 2 2 DNC 2 DNC 2 DNC 2 DNC 2");
        Assert.Equal(28m, c1.TotalScore);
        Assert.Equal(1m, c1.ColumnValues[ScoringColumns.PenaltiesKey].Value);
        Assert.Equal(8m, c1.ColumnValues[ScoringColumns.PenaltiesKey].Multiplier);
        Assert.Equal(60m, c1.ParticipationPercent);
    }

    [Fact]
    public void CalculateResults_SeasonRanks_LowPointsWin()
    {
        var results = ScoreSeason();

        ScenarioAssert.Totals(results, ("C0", 10), ("C3", 30));
        ScenarioAssert.Rank(results, "C0", 1);
        ScenarioAssert.Rank(results, "C1", 2);
        ScenarioAssert.Rank(results, "C3", 3);
        Assert.False(results.For("C0").ColumnValues.ContainsKey(ScoringColumns.PenaltiesKey));
        Assert.True(results.Results.LowerScoreWins);
        Assert.Equal(0, results.Results.NumberOfDiscards);
    }

    [Fact]
    public void CalculateResults_ExcusedAbsences_AvoidPenaltyButNotParticipationRequirement()
    {
        var results = ScoreSeason();

        // Four 3rds started (40%, under 51%), plus two excused: six sailed, one short of 7.
        var c2 = results.For("C2");
        Assert.Equal(3m, results.Score("C2", 5).ScoreValue);
        Assert.Equal(40m, c2.ParticipationPercent);
        Assert.Null(c2.TotalScore);
        ScenarioAssert.Rank(results, "C2", null);
        Assert.Equal(1m, c2.ColumnValues[ScoringColumns.PenaltiesKey].Value);
        Assert.Equal(38m, c2.PointsEarned);
    }

    [Fact]
    public void CalculateResults_Breakdown_ScoresAverageAndCountsAsStarted()
    {
        var results = Wawasee()
            .Race("C0:1 C1:2 C2:3 C3:4")
            .Race("C0:1 C1:4 C2:2 C3:3")
            .Race("C0:1 C1:BKD C2:2 C3:3")
            .Score();

        ScenarioAssert.Scores(results, "C1", "2 4 BKD 3");
        Assert.Equal(3m, results.For("C1").Average);
        Assert.Equal(100m, results.For("C1").ParticipationPercent);
        ScenarioAssert.Totals(results, ("C1", 9));
    }

    [Fact]
    public void CalculateResults_DayWithThreeBoats_RacesDoNotCount()
    {
        // Day 2: only three boats started.
        var results = Wawasee()
            .Race("C0:1 C1:2 C2:3 C3:4 C4:5")
            .Race("C0:1 C1:2 C2:3 C3:4 C4:5")
            .NextDay()
            .Race("C0:1 C1:2 C2:3")
            .Score();

        Assert.True(results.Score("C0", 3).Discard);
        ScenarioAssert.Totals(results, ("C0", 2), ("C3", 8));
        // C3 sailed both races that count.
        Assert.Equal(100m, results.For("C3").ParticipationPercent);
        Assert.Equal(1, results.Results.NumberOfDiscards);
    }

    [Fact]
    public void CalculateResults_FourBoatsStartedDuringTheDay_RacesCount()
    {
        // Day 2: three boats in each race, but four different boats started a race.
        var results = Wawasee()
            .Participation(0)
            .Race("C0:1 C1:2 C2:3 C3:4")
            .NextDay()
            .Race("C0:1 C1:2 C2:3")
            .Race("C0:1 C1:2 C3:3")
            .Score();

        Assert.DoesNotContain(results.Results.Results.Values.SelectMany(r => r.CalculatedScores.Values), s => s.Discard);
        ScenarioAssert.Totals(results, ("C0", 3));
    }

    [Fact]
    public void CalculateResults_RequiredRacesBelowHalf_RoundUp()
    {
        // Twelve races at 70% is 8.4 races. Eight would be under 70%, so 9 are needed:
        // C1 sailed 8, so one penalty.
        var scenario = Wawasee().Participation(0).Settings(s => s.MinimumCompetitors = null);
        for (var i = 0; i < 8; i++)
        {
            scenario.Race("C0:1 C1:2");
        }
        for (var i = 0; i < 4; i++)
        {
            scenario.Race("C0:1");
        }

        var results = scenario.Score();

        Assert.Equal(1m, results.For("C1").ColumnValues[ScoringColumns.PenaltiesKey].Value);
    }

    [Fact]
    public void CalculateResults_RequiredRacesAtHalf_RoundUp()
    {
        // Five races at 70% is 3.5 races: 4 are needed. C1 sailed 3, so one penalty.
        var results = Wawasee().Participation(0).Settings(s => s.MinimumCompetitors = null)
            .Race("C0:1 C1:2").Race("C0:1 C1:2").Race("C0:1 C1:2")
            .Race("C0:1").Race("C0:1")
            .Score();

        Assert.Equal(1m, results.For("C1").ColumnValues[ScoringColumns.PenaltiesKey].Value);
    }

    [Fact]
    public void CalculateResults_TiedPoints_LowerAverageFinishWins()
    {
        // A (C0): 3, 3, 3, 1 = 10, average 2.5.
        // B (C1): 2, 2, 2, missed (2) = 8, plus one race short at 2 points = 10, average 2.
        var results = Wawasee(penaltyPoints: 2m)
            .Participation(0)
            .Settings(s => { s.PenaltyThresholdPercent = 100m; s.MinimumCompetitors = null; })
            .Race("C0:3 C1:2 C2:1")
            .Race("C0:3 C1:2 C2:1")
            .Race("C0:3 C1:2 C2:1")
            .Race("C0:1 C2:2")
            .Score();

        ScenarioAssert.Totals(results, ("C0", 10), ("C1", 10));
        ScenarioAssert.Ranks(results, "C2", "C1", "C0");
    }
}
