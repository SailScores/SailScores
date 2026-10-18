using SailScores.Core.Model;

namespace SailScores.Core.Scoring
{
    public interface IScoringCalculator
    {
        SeriesResults CalculateResults(Series series);

        // The series score of a competitor who missed every race, scored against the
        // series' competitors; null when the scoring system gives such a competitor no score.
        decimal? CalculateAbsentCompetitorScore(Series series);
    }
}
