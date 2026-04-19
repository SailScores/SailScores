using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicSeriesScoreCodeDto
    {
        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        [StringLength(100)]
        public string Formula { get; set; }
    }
}
