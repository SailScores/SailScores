using System;

namespace SailScores.Core.Model
{
    public class Score
    {
        public Guid Id { get; set; }
        public Competitor Competitor { get; set; }
        public Guid CompetitorId { get; set; }
        public Race Race { get; set; }
        public Guid RaceId { get; set; }
        public int? Place { get; set; }
        public string Code { get; set; }
        public decimal? CodePoints { get; set; }

        // New fields for timing
        public DateTime? FinishTime { get; set; }
        public TimeSpan? ElapsedTime { get; set; }
    }
}
