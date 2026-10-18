using SailScores.Core.Model;
using System;
using System.Globalization;
using System.Text;

namespace SailScores.Core.Scoring
{
    // CalculatedScore is results for a particular series. (competitor and race)
    public class CalculatedScore
    {
        // The scoring run's copy of the competitor's score in the race, as scored: its place
        // may be cleared and its code may be a score code group's overage code. Results
        // show Place and Code.
        public Score RawScore { get; set; }

        // The place shown: none for a code that doesn't keep the result (PreserveResult off);
        // the corrected-time place under a handicap system.
        public int? Place { get; set; }

        // The code shown: the code entered, even when the score was scored as an overage code.
        public string Code { get; set; }
        public Decimal? ScoreValue { get; set; }
        // for Cox-Sprague: the score value of first place
        public Decimal? PerfectScoreValue { get; set; }
        public bool Discard { get; set; }
        public bool? CountsAsParticipation { get; set; }

        // A score taken as-is from a child series (summary series): rules leave it alone.
        public bool IsPresetScore => RawScore?.PresetScoreValue.HasValue == true;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Code + " ");

            var placePart = ScoreValue?.ToString("N1", CultureInfo.CurrentCulture) ??
                Place?.ToString("N1", CultureInfo.CurrentCulture);
            sb.Append(placePart);

            return sb.ToString().Trim();
        }
    }
}