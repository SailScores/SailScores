using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring;

// What one scoring run knows beyond the scores themselves. A calculator starts a new
// workspace for every run (each CalculateResults, trend rerun and absent-competitor score),
// and hands it to its rules, which keep no state of their own.
public sealed class ScoringWorkspace
{
    public ScoringSystem System { get; init; }

    // How score codes are scored in this run.
    public Formulas.FormulaSet Formulas { get; init; }

    // Answers that depend on the calculator: score codes, default scores, averages.
    public IScoringHost Host { get; init; }

    // The discard method for this series, with ChildSeriesDiscardsOrPattern resolved.
    public DiscardMethod DiscardMethod { get; init; }

    // True when places are taken from the whole race: the series has no fleet, or uses full
    // race scores. False when places are recalculated among the fleet's boats.
    public bool UseOriginalPlace { get; init; }

    // True when a fleet series uses full race scores: the boats' places in the whole race.
    public bool UseFullRaceScores { get; init; }

    // Races whose scores don't count (Wawasee 2026 and VARC 2026 minimum-boat rules).
    // Filled in by those rules as competitors are scored.
    public ISet<Race> RacesNotCounted { get; } = new HashSet<Race>();

    // This run's copy of each race's scores. Scoring changes the copies (places cleared,
    // overage codes, handicap places), never the series' own scores, so rules read a race's
    // scores with ScoresIn, not Race.Scores.
    public IReadOnlyDictionary<Race, IReadOnlyList<Score>> RaceScores { get; init; }
        = new Dictionary<Race, IReadOnlyList<Score>>();

    // The code each score code group overage replaced, by score: results show it.
    public IDictionary<Score, string> CodesBeforeOverage { get; }
        = new Dictionary<Score, string>(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<Score> ScoresIn(Race race) =>
        race != null && RaceScores.TryGetValue(race, out var scores) ? scores : [];

    // The race's scores that count for boat numbers: every score, or only the fleet's when
    // places are recalculated within a fleet.
    public IEnumerable<Score> RaceScoresForCounts(Race race, SeriesResults results) =>
        UseOriginalPlace || results == null
            ? ScoresIn(race)
            : ScoresIn(race).Where(s => results.Competitors.Any(c => c.Id == s.CompetitorId));
}
