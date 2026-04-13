using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicKeyValueOptionDto
    {
        [Required]
        [StringLength(200)]
        public string Key { get; set; }

        [Required]
        [StringLength(200)]
        public string Value { get; set; }
    }
}
