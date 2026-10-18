using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring.Characterization;

// Rules that hold for any correct result, checked over every fixture and calculator
// variant. Unlike snapshots, these catch a wrong result that was wrong from the start.
public class ScoringInvariantTests
{
    // Calculators whose total is the sum of the counted race scores, lowest wins.
    private static readonly HashSet<ScoringCalculatorType> LowPointSumCalculators =
    [
        ScoringCalculatorType.AppendixA,
        ScoringCalculatorType.AppendixAPre2025,
        ScoringCalculatorType.AppAAltFirstIsZero,
        ScoringCalculatorType.AppAAltFirstIsPoint7,
        ScoringCalculatorType.AppAAltFirstIsPoint75
    ];

    public static TheoryData<string, string> AllCases()
    {
        var data = new TheoryData<string, string>();
        foreach (var fixture in ScoringFixture.Names())
        {
            data.Add(fixture, null);
        }
        foreach (var (fixture, sharedSystem) in ScoringFixtureTests.CalculatorVariantCases())
        {
            data.Add(fixture, sharedSystem);
        }
        return data;
    }

    // Standard competition ranking: tied competitors share a rank and the next rank
    // skips, as in 1, 2, 2, 4.
    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task Ranks_AnyFixture_FollowCompetitionRanking(string fixture, string sharedSystem)
    {
        var results = (await FixtureScorer.ScoreAsync(fixture, sharedSystem)).Results;
        var ranks = results.CalculatedScores.Where(s => s.Rank.HasValue).Select(s => s.Rank.Value).ToList();

        foreach (var rank in ranks.Distinct())
        {
            Assert.Equal(1 + ranks.Count(r => r < rank), rank);
        }
    }

    // A total of 0 ranks last: LowestTotal(zeroTotalRanksLast: true) treats it as "no scores". See
    // AppAAltFirstIsZero_CompetitorWhoWonEveryCountedRace_RanksFirst for where that's wrong.
    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task Ranks_LowPointSumCalculator_OrderedByTotal(string fixture, string sharedSystem)
    {
        var scored = await FixtureScorer.ScoreAsync(fixture, sharedSystem);
        Assert.SkipUnless(IsLowPointSum(scored), "Not a low point sum calculator.");

        var totals = Ranked(scored.Results)
            .Select(s => s.TotalScore is null or 0 ? decimal.MaxValue : s.TotalScore.Value)
            .ToList();

        Assert.Equal(totals.Order(), totals);
    }

    // Known bug, left alone because fixing it changes saved results. Under First = 0 a win
    // scores 0, so a boat that wins every counted race totals 0, and the comparer ranks a 0
    // total last. Its tiebreak also ignores 0 scores, so wins don't count there either.
    [Fact(Skip = "Known bug in LowestTotal(zeroTotalRanksLast: true): a total of 0 ranks last. Needs maintainer approval to fix.")]
    public async Task AppAAltFirstIsZero_CompetitorWhoWonEveryCountedRace_RanksFirst()
    {
        var scored = await FixtureScorer.ScoreAsync("gyc_2026_helm-of-the-year", "Appendix A Alternative - First = 0");
        // The boat that won every counted race (fixture names are anonymized, so find it by id).
        var winner = scored.Results.Competitors.Single(c => c.Id == new Guid("e0ca124e-49bc-43d0-a476-e00685800adc"));

        Assert.Equal(1, scored.Results.CalculatedScores.Single(s => s.CompetitorId == winner.Id).Rank);
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task Total_LowPointSumCalculator_IsSumOfCountedScores(string fixture, string sharedSystem)
    {
        var scored = await FixtureScorer.ScoreAsync(fixture, sharedSystem);
        Assert.SkipUnless(IsLowPointSum(scored), "Not a low point sum calculator.");

        foreach (var score in Ranked(scored.Results))
        {
            Assert.Equal(
                score.Scores.Where(s => !s.Discard).Sum(s => s.ScoreValue ?? 0),
                score.TotalScore);
            Assert.Equal(
                score.Scores.Sum(s => s.ScoreValue ?? 0),
                score.TotalScoreIncludingDiscards);
        }
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task Discards_RaceCountPattern_NoMoreThanAllowed(string fixture, string sharedSystem)
    {
        var results = (await FixtureScorer.ScoreAsync(fixture, sharedSystem)).Results;
        Assert.SkipWhen(
            results.DiscardMethod is not (null or DiscardMethod.RaceCountPattern),
            "Discards are not one count for the whole series.");

        foreach (var score in results.CalculatedScores)
        {
            Assert.True(
                score.Scores.Count(s => s.Discard) <= results.NumberOfDiscards,
                $"Competitor {score.CompetitorId} has more than {results.NumberOfDiscards} discards.");
        }
    }

    // Every competitor with a score in one of the series' own races gets a result.
    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task Competitors_StandardSeries_AllScoredCompetitorsHaveResults(string fixture, string sharedSystem)
    {
        var scored = await FixtureScorer.ScoreAsync(fixture, sharedSystem);
        Assert.SkipWhen(scored.IsSummarySeries, "Summary series score child series, not races.");

        var withResults = scored.Results.CalculatedScores.Select(s => s.CompetitorId).ToHashSet();

        Assert.Equal(
            scored.Fixture.Backup.Races
                .Where(r => r.SeriesIds?.Count > 0)
                .SelectMany(r => r.Scores ?? [])
                .Select(s => s.CompetitorId)
                .Distinct()
                .Count(),
            withResults.Count);
    }

    private static bool IsLowPointSum(ScoredFixture scored) =>
        scored.CalculatorType is { } type && LowPointSumCalculators.Contains(type);

    private static IEnumerable<FlatSeriesScore> Ranked(FlatResults results) =>
        results.CalculatedScores.Where(s => s.Rank.HasValue).OrderBy(s => s.Rank);
}
