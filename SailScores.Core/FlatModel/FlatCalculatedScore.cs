using System;
using System.Text;

namespace SailScores.Core.FlatModel
{
    public class FlatCalculatedScore
    {
        public Guid RaceId { get; set; }
        public int? Place { get; set; }
        public string Code { get; set; }
        public Decimal? ScoreValue { get; set; }
        public bool Discard { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Code + " ");

            var placePart = ScoreValue?.ToString("N1") ??
                Place?.ToString("N1");
            sb.Append(placePart);

            return sb.ToString().Trim();
        }
    }
}