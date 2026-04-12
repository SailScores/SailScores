using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicSeriesCompetitorDto
    {
        [Required]
        public Guid Id { get; set; }

        [Range(1, int.MaxValue)]
        public int? Rank { get; set; }

        [Required]
        [StringLength(200)]
        public string CompetitorName { get; set; }

        [Required]
        [StringLength(200)]
        public string BoatName { get; set; }

        [StringLength(50)]
        public string SailNumber { get; set; }

        [StringLength(50)]
        public string TotalPoints { get; set; }

        [Required]
        [StringLength(500)]
        public string Url { get; set; }
    }
}
