using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicApiRootResponseDto
    {
        [Required]
        [StringLength(20)]
        public string Version { get; set; }

        [Required]
        [StringLength(500)]
        public string ClubsIndexUrl { get; set; }
    }
}
