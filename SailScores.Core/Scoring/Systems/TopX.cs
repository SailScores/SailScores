using SailScores.Core.Model;
using SailScores.Core.Scoring.Adjustments;
using SailScores.Core.Scoring.Discards;
using SailScores.Core.Scoring.Formulas;
using SailScores.Core.Scoring.Glossary;
using SailScores.Core.Scoring.RacePoints;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Core.Scoring.Totals;

namespace SailScores.Core.Scoring.Systems;

// Top X high point, and VARC 2026 built on it.
public static class TopX
{
    // Top X High Point.
    //   - First scores 100, second 99..., less one per starter short of five.
    //   - Coded results that came to the start score one behind the last starter; codes that
    //     didn't, and missing results, score 0.
    //   - No discards: the total is the boat's best X scores, X being stored in the system's
    //     participation percent field (5 if blank).
    //   - High total wins; ties go to more races sailed, then A8.1, then A8.2.
    public static ScoringRecipe Create(ScoringSystem system) => new()
    {
        Name = "Top X High Point",
        RacePoints = new TopXPoints(),
        Formulas = FormulaSet.HighPoint,
        CodedResults = new CodedResultsOneBehindLast(TopXPoints.ForPlace),
        FixedDefaultScore = 0m,
        Adjustments = [new RaiseToDefaultScore()],
        Discards = [new NoDiscards()],
        Total = new TopXBestScores(),
        Eligibility = [],
        Ranking = Rankings.HighPoint,
        Glossary = new TopXGlossary()
    };

    // VARC 2026 and VARC 2026 Shorthanded: Top X race scores (DNC 0), but the total is each
    // boat's best ScoresToCount scores; the others show as discards and are left out of A8.1.
    // VARC 2026 also registers MinimumCompetitors (a race with fewer boats that came to the
    // start doesn't count: its scores are discards and A8.2 skips it) and TiesStandAfterA81.
    //   - High total wins; ties go to A8.1 over the scores that count, then (unless ties
    //     stand after A8.1) A8.2 skipping races that didn't count.
    public static ScoringRecipe Varc2026(ScoringSystem system)
    {
        var ranking = Ranking.By(new HighestTotal()).Then(new CountedScoresBestFirst());
        var tiesStandAfterA81 = system?.EffectiveSettings?.TiesStandAfterA81 ?? false;
        return new()
        {
            Name = "VARC 2026",
            RacePoints = new TopXPoints(),
            Formulas = FormulaSet.HighPoint,
            CodedResults = new CodedResultsOneBehindLast(TopXPoints.ForPlace),
            FixedDefaultScore = 0m,
            Adjustments = [new RaiseToDefaultScore(), new NotAtStartScoresFixedOrZero()],
            Discards = [new RacesNeedBoatsAtStart(), new KeepBestScores()],
            Total = new HighPointSum(),
            Eligibility = [],
            Ranking = tiesStandAfterA81 ? ranking : ranking.Then(new LastRaceSkippingUncounted()),
            Glossary = new TopXGlossary()
        };
    }
}
