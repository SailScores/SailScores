using SailScores.Core.Model;

namespace SailScores.Core.Scoring
{
    public interface ISeriesCalculator
    {
        SeriesResults CalculateResults(Series series);
    }
}
