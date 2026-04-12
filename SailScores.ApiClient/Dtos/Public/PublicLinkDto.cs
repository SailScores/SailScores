using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicLinkDto
    {
        [Required]
        [StringLength(100)]
        public string Rel { get; set; }

        [Required]
        [StringLength(500)]
        public string Href { get; set; }
    }
}
