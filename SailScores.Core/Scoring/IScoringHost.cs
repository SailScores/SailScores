using SailScores.Core.Model;
using SailScores.Core.Scoring.Formulas;

namespace SailScores.Core.Scoring;

// What scoring rules can ask of the calculator scoring the series.
public interface IScoringHost : IFormulaHost
{
    // The code's definition, following the calculator's lookup (an unknown code is scored
    // as the default code, DNC). Null for a score without a code.
    ScoreCode GetScoreCode(Score score);

    ScoreCode GetScoreCode(string codeName);

    // A code that moves the boats behind up a place (DSQ, DNF...): not counted as ahead.
    bool AdjustsOtherScores(Score score);

    bool CountsAsParticipation(Score score);

    bool IsIncludedInAverage(Score score);

    bool IsAverage(string codeName);

    // The score for no result in this race (the default code's score, DNC).
    decimal? DefaultScore(Race race, SeriesResults results);

    // A FIX or MAN code's fixed or entered value; null for other formulas.
    decimal? FixedOrManualValue(CalculatedScore score, ScoreCode code);

    // Discards the discard pattern gives for this many races.
    int DiscardsForRaceCount(int raceCount);
}
