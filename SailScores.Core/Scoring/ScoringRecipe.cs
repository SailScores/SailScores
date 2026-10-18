using System.Collections.Generic;
using SailScores.Core.Scoring.Adjustments;
using SailScores.Core.Scoring.Discards;
using SailScores.Core.Scoring.Eligibility;
using SailScores.Core.Scoring.Formulas;
using SailScores.Core.Scoring.Glossary;
using SailScores.Core.Scoring.RacePoints;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Core.Scoring.Totals;

namespace SailScores.Core.Scoring;

// How one scoring system scores: the rule for each step of ScoringPipeline. Each system's
// recipe is built in Scoring/Systems/ and registered in ScoringCalculatorRegistry.
//
// The steps, in the order they run for each competitor, then for the series:
//   RacePoints   points for each finish
//   (code limits, missing results scored as DNC: the same for every system)
//   Formulas     score code formulas (FIX, PLC%, AVE...)
//   Adjustments  changes after formulas: caps, club rules
//   Discards     which scores are discarded
//   Total        series totals
//   Eligibility  who can be ranked
//   Ranking      the order, with tiebreaks
public sealed class ScoringRecipe
{
    // Shown in tests and diagnostics, e.g. "Appendix A, First = 0".
    public required string Name { get; init; }

    public required IRacePointsRule RacePoints { get; init; }

    public FormulaSet Formulas { get; init; } = FormulaSet.Standard;

    // Replaces the race step of the formulas (Cox-Sprague, Top X). Null: formulas as usual.
    public ICodedResultsRule CodedResults { get; init; }

    // False: series-based and average formulas (SER+, AVE...) aren't evaluated, and the
    // default score isn't taken from a series count (Cox-Sprague).
    public bool SeriesFormulas { get; init; } = true;

    // The score for no result, when it isn't the DNC code's (Top X: 0).
    public decimal? FixedDefaultScore { get; init; }

    // Which scores count in an average when the code doesn't say.
    public AveragedScores AveragedScores { get; init; } = AveragedScores.All;

    public required IReadOnlyList<IScoreAdjustment> Adjustments { get; init; }

    public required IReadOnlyList<IDiscardRule> Discards { get; init; }

    public required ITotalRule Total { get; init; }

    public required IReadOnlyList<IEligibilityRule> Eligibility { get; init; }

    public required Ranking Ranking { get; init; }

    // The formula text shown for each code in results.
    public ICodeGlossary Glossary { get; init; } = new StandardGlossary();

    public override string ToString() => Name;
}
