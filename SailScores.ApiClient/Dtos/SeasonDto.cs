using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos
{
    public class SeasonDto
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }

        [StringLength(200)]
        public String Name { get; set; }

        [StringLength(200)]
        public String UrlName { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        // Seasons have default scoring system as well, but not
        // currently included in this DTO

        public IEnumerable<Guid> SeriesIds { get; set; }
    }
}
