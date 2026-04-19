using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SailScores.Api.Dtos.Public
{
    public class PublicSeriesCompetitorDto
    {
        [Required]
        public Guid Id { get; set; }

        [Range(1, int.MaxValue)]
        public int? Rank { get; set; }

        public int? Trend { get; set; }

        [Required]
        [StringLength(200)]
        public string CompetitorName { get; set; }

        [Required]
        [StringLength(200)]
        public string BoatName { get; set; }

        [StringLength(50)]
        public string SailNumber { get; set; }

        [StringLength(50)]
        public string AlternativeSailNumber { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [StringLength(200)]
        public string HomeClubName { get; set; }

        [StringLength(50)]
        public string TotalPoints { get; set; }

        [Required]
        [StringLength(500)]
        public string Url { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<PublicSeriesCompetitorRaceResultDto> RaceResults { get; set; }
    }
}
