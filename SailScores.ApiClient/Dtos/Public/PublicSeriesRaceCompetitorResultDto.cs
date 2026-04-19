using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicSeriesRaceCompetitorResultDto
    {
        [Required]
        public Guid CompetitorId { get; set; }

        [Range(1, int.MaxValue)]
        public int? Place { get; set; }

        [StringLength(20)]
        public string Code { get; set; }

        public decimal? ScoreValue { get; set; }

        public decimal? PerfectScoreValue { get; set; }

        public bool Discard { get; set; }

        public TimeSpan? ElapsedTime { get; set; }

        public DateTimeOffset? FinishTimeUtc { get; set; }
    }
}
