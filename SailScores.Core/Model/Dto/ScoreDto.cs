using System;

namespace SailScores.Core.Model.Dto
{
    public class ScoreDto
    {
        public Guid Id { get; set; }
        public Guid CompetitorId { get; set; }
        public Guid RaceId { get; set; }
        public int? Place { get; set; }
        public string Code { get; set; }
    }
}
