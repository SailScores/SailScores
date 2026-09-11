using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicSeriesScoreCodeGroupDto
    {
        [Required]
        [StringLength(500)]
        public string Description { get; set; }
    }
}
