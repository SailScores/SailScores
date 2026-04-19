using SailScores.Api.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicRaceDetailResponseDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(10)]
        public string ClubInitials { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        public DateTimeOffset? DateUtc { get; set; }

        public int Order { get; set; }

        public RaceState? State { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(500)]
        public string HtmlUrl { get; set; }

        [StringLength(30)]
        public string WindSpeed { get; set; }

        [StringLength(30)]
        public string WindSpeedUnits { get; set; }

        public decimal? WindDirectionDegrees { get; set; }

        [StringLength(50)]
        public string WeatherIcon { get; set; }

        public DateTimeOffset? UpdatedUtc { get; set; }

        [StringLength(200)]
        public string UpdatedBy { get; set; }

        public IList<PublicRaceCompetitorResultDto> CompetitorResults { get; set; } =
            new List<PublicRaceCompetitorResultDto>();
    }
}
