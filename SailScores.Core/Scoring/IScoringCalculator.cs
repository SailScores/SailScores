using SailScores.Core.Model;

namespace SailScores.Core.Scoring
{
    public interface IScoringCalculator
    {
        SeriesResults CalculateResults(Series series);
    }
}
