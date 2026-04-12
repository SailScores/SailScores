using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicSeasonListItemDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(10)]
        public string ClubInitials { get; set; }

        [Required]
        [StringLength(200)]
        public string SeasonName { get; set; }

        [Required]
        [StringLength(200)]
        public string SeasonUrlName { get; set; }

        [Required]
        [StringLength(500)]
        public string Url { get; set; }

        public DateTimeOffset? UpdatedUtc { get; set; }
    }
}
