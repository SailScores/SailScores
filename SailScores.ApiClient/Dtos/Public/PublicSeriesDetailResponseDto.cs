using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SailScores.Api.Dtos.Public
{
    public class PublicSeriesDetailResponseDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string UrlName { get; set; }

        [StringLength(500)]
        public string HtmlUrl { get; set; }

        [Required]
        [StringLength(10)]
        public string ClubInitials { get; set; }

        [Required]
        [StringLength(200)]
        public string SeasonName { get; set; }

        [Required]
        [StringLength(200)]
        public string SeasonUrlName { get; set; }

        public DateTimeOffset? UpdatedUtc { get; set; }

        [Required]
        public IList<PublicLinkDto> Links { get; set; } = new List<PublicLinkDto>();

        [Required]
        public IList<PublicSeriesCompetitorDto> Competitors { get; set; } = new List<PublicSeriesCompetitorDto>();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<PublicSeriesRaceItemDto> Races { get; set; }
    }
}
