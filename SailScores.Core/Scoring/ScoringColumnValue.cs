using System;

namespace SailScores.Core.Scoring
{
    // One competitor's value in a scoring column. Shown as Value, or as
    // "Value × Multiplier" when Multiplier is set (e.g. 2 penalties × 1.5 points).
    public class ScoringColumnValue
    {
        public Decimal? Value { get; set; }
        public Decimal? Multiplier { get; set; }
    }
}
