using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos.Public
{
    public class PublicSeriesRaceItemDto
    {
        [Required]
        public Guid Id { get; set; }

        public DateTimeOffset DateUtc { get; set; }

        public int Order { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Url { get; set; }

        [StringLength(500)]
        public string HtmlUrl { get; set; }

    }
}
