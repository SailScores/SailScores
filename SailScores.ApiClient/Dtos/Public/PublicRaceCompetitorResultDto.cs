using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicRaceCompetitorResultDto
    {
        [Required]
        public Guid CompetitorId { get; set; }

        [StringLength(200)]
        public string CompetitorName { get; set; }

        [Range(1, int.MaxValue)]
        public int? Place { get; set; }

        [StringLength(20)]
        public string Code { get; set; }

        public decimal? CodePoints { get; set; }

        public DateTimeOffset? FinishTimeUtc { get; set; }

        public TimeSpan? ElapsedTime { get; set; }
    }
}
