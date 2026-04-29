using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SailScores.Api.Enumerations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicSeriesRaceItemDto
    {
        [Required]
        public Guid Id { get; set; }

        public DateTimeOffset? DateUtc { get; set; }

        public int Order { get; set; }

        public RaceState? State { get; set; }

        [StringLength(30)]
        public string WindSpeed { get; set; }

        [StringLength(30)]
        public string WindSpeedUnits { get; set; }

        public decimal? WindDirectionDegrees { get; set; }

        [StringLength(50)]
        public string WeatherIcon { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Url { get; set; }

        [StringLength(500)]
        public string HtmlUrl { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<PublicSeriesRaceCompetitorResultDto> CompetitorResults { get; set; }

    }
}
