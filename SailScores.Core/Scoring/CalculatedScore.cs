using SailScores.Core.Model;
using System;
using System.Globalization;
using System.Text;

namespace SailScores.Core.Scoring
{
    // CalculatedScore is results for a particular series. (competitor and race)
    public class CalculatedScore
    {
        // a raw score is per competitor and race, regardless of series.
        public Score RawScore { get; set; }
        public Decimal? ScoreValue { get; set; }
        public bool Discard { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(RawScore?.Code + " ");

            var placePart = ScoreValue?.ToString("N1", CultureInfo.CurrentCulture) ??
                RawScore?.Place?.ToString("N1", CultureInfo.CurrentCulture);
            sb.Append(placePart);
            
            return sb.ToString().Trim();
        }
    }
}