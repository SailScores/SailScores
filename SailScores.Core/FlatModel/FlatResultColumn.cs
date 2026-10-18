using SailScores.Api.Enumerations;
using SailScores.Core.Scoring;

namespace SailScores.Core.FlatModel
{
    // A scoring column as it was when the results were calculated, with its visibility
    // resolved from the scoring system. Values are in FlatSeriesScore.ColumnValues by Key.
    public class FlatResultColumn
    {
        public string Key { get; set; }
        public string Header { get; set; }
        public ScoringColumnFormat Format { get; set; }
        public ColumnVisibility Visibility { get; set; }
    }
}
