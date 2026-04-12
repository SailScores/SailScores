using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicApiRootResponseDto
    {
        [Required]
        [StringLength(20)]
        public string Version { get; set; }

        [Required]
        public IList<PublicLinkDto> Links { get; set; } = new List<PublicLinkDto>();
    }
}
