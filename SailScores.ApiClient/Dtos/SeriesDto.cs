﻿using SailScores.Api.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos
{
    public class SeriesDto
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }

        [StringLength(200)]
        public String UrlName { get; set; }

        [StringLength(2000)]
        public String Description { get; set; }

        public IList<Guid> RaceIds { get; set; }

        [Required]
        public Guid SeasonId { get; set; }

        public bool? IsImportantSeries { get; set; }

        public bool? ResultsLocked { get; set; }

        public DateTime? UpdatedDate { get; set; }
        [StringLength(128)]
        public String UpdatedBy { get; set; }

        public TrendOption? TrendOption { get; set; }

    }
}
