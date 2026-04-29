using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicClubDetailResponseDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(10)]
        public string ClubInitials { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        [Required]
        [StringLength(500)]
        public string Url { get; set; }

        [StringLength(500)]
        public string HtmlUrl { get; set; }

        public DateTimeOffset? UpdatedUtc { get; set; }
    }
}
