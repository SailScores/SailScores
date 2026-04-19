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

        [StringLength(2000)]
        public string Description { get; set; }

        [StringLength(50)]
        public string SeriesType { get; set; }

        [StringLength(200)]
        public string FleetName { get; set; }

        [StringLength(100)]
        public string TrendOption { get; set; }

        public bool PreferAlternativeSailNumbers { get; set; }

        public bool HideDncDiscards { get; set; }

        public bool? IsPreliminary { get; set; }

        public int NumberOfSailedRaces { get; set; }

        public int NumberOfDiscards { get; set; }

        public int CompetitorCount { get; set; }

        [StringLength(200)]
        public string ScoringSystemName { get; set; }

        public decimal? PercentRequired { get; set; }

        [StringLength(200)]
        public string UpdatedBy { get; set; }

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

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<PublicSeriesCompetitorDto> Competitors { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<PublicSeriesRaceItemDto> Races { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<PublicSeriesScoreCodeDto> ScoreCodesUsed { get; set; }
    }
}
