using SailScores.Core.FlatModel;
using SailScores.Core.Scoring;

namespace SailScores.Web.Extensions;

public static class ScoringColumnExtensions
{
    // Empty when the competitor has no value for the column.
    public static string GetDisplayText(this FlatResultColumn column, FlatSeriesScore score)
    {
        if (column == null || score?.ColumnValues == null
            || !score.ColumnValues.TryGetValue(column.Key, out var value)
            || value?.Value == null)
        {
            return string.Empty;
        }
        var text = column.Format == ScoringColumnFormat.Percent
            ? $"{value.Value:0.#}%"
            : $"{value.Value:0.###}";
        return value.Multiplier.HasValue
            ? $"{text} × {value.Multiplier:0.###}"
            : text;
    }
}
