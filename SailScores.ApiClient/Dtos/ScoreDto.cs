using System;

namespace SailScores.Api.Dtos
{
    public class ScoreDto
    {
        public Guid Id { get; set; }
        public Guid CompetitorId { get; set; }
        public Guid RaceId { get; set; }
        public int? Place { get; set; }
        public string Code { get; set; }
        public decimal? CodePoints { get; set; }
        // New fields
        public DateTime? FinishTime { get; set; }
        public TimeSpan? ElapsedTime { get; set; }
        public override string ToString()
        {
            return Id + " : " + Place + " : " + Code + " : " + CompetitorId;
        }
    }
}
