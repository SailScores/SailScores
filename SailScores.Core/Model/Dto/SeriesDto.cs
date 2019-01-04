using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SailScores.Core.Scoring;

namespace SailScores.Core.Model.Dto
{
    public class SeriesDto
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }

        [StringLength(2000)]
        public String Description { get; set; }
        
        public IList<Guid> RaceIds { get; set; }

        [Required]
        public Guid SeasonId { get; set; }

    }
}
