using System.Collections.Generic;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// What a formula is evaluating: one competitor's score in one race.
public sealed class FormulaContext
{
    public IFormulaHost Host { get; init; }

    public CalculatedScore Score { get; init; }

    // The score's code definition.
    public ScoreCode Code { get; init; }

    public Race Race { get; init; }

    // Null when a code's value is worked out outside a series run (the DNF score for a
    // penalty can be).
    public SeriesResults Series { get; init; }

    // The competitor's results so far. Set for Series and Average formulas.
    public SeriesCompetitorResults Competitor { get; init; }
}

// What a formula can ask of the calculator scoring the series. The answers depend on the
// calculator's score codes and on whether places are recalculated within a fleet.
public interface IFormulaHost
{
    // The formula for a score code name, following the calculator's lookup (an unknown code
    // is scored as DNC). Null for a blank code.
    IScoreCodeFormula FormulaFor(string codeName);

    bool CountsAsFinished(Score score);

    bool CameToStart(Score score);

    // The race's scores that count for boat numbers: every score, or only the fleet's when
    // places are recalculated within a fleet.
    IEnumerable<Score> RaceScores(Race race, SeriesResults series);

    decimal? DnfScore(Race race, SeriesResults series);

    // Discards the competitor is expected to have, for averages of non-discarded races.
    int ExpectedDiscards(SeriesCompetitorResults competitor);
}
