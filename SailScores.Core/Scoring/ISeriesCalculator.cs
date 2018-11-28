using Sailscores.Core.Model;

namespace Sailscores.Core.Scoring
{
    public interface ISeriesCalculator
    {
        SeriesResults CalculateResults(Series series);
    }
}
