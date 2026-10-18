using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring.Characterization;

// Characterization tests: score real series and compare with approved snapshots in
// Snapshots/ (see Snapshot for how to accept a change). A failure means scoring output
// changed. Never accept a change to an existing calculator's output without the maintainer's
// approval: saved results are recalculated.
public class ScoringFixtureTests
{
    // Fixtures re-scored with every shared base system, to cover calculators no club
    // fixture uses. Chosen for many codes and a big fleet, ties, and a summary series.
    private static readonly string[] CalculatorVariantFixtures =
    [
        "lhyc_2018_mc-holiday",
        "lhyc_2025_2025-mc-schepers-memorial-chase",
        "gyc_2026_helm-of-the-year"
    ];

    public static TheoryData<string> Fixtures() => new(ScoringFixture.Names());

    public static IEnumerable<(string Fixture, string SharedSystem)> CalculatorVariantCases() =>
        from fixture in CalculatorVariantFixtures
        from system in ScoringFixture.SharedSystems().Select(s => s.Name).Order()
        select (fixture, system);

    public static TheoryData<string, string> CalculatorVariants() => new(CalculatorVariantCases());

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task Score_ClubFixture_MatchesSnapshot(string fixture)
    {
        var scored = await FixtureScorer.ScoreAsync(fixture);

        await Snapshot.MatchAsync(ResultsSnapshot.ToJson(scored.Results), fixture);
    }

    [Theory]
    [MemberData(nameof(CalculatorVariants))]
    public async Task Score_FixtureWithSharedSystem_MatchesSnapshot(string fixture, string sharedSystem)
    {
        var scored = await FixtureScorer.ScoreAsync(fixture, sharedSystem);

        await Snapshot.MatchAsync(
            ResultsSnapshot.ToJson(scored.Results),
            $"{fixture}__{FileNamePart(sharedSystem)}",
            "SharedSystems");
    }

    // Restore gives every entity a new id, so anything that orders by id would make
    // snapshots flaky. Ties are where that would show.
    [Fact]
    public async Task Score_SameFixtureTwice_GivesSameResults()
    {
        const string fixtureWithTies = "lhyc_2025_2025-mc-schepers-memorial-chase";

        var first = await FixtureScorer.ScoreUncachedAsync(fixtureWithTies);
        var second = await FixtureScorer.ScoreUncachedAsync(fixtureWithTies);

        Assert.Equal(ResultsSnapshot.ToJson(first.Results), ResultsSnapshot.ToJson(second.Results));
    }

    private static string FileNamePart(string name) =>
        new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
}
