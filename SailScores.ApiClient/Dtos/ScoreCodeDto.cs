﻿using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos
{
    public class ScoreCodeDto
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }
        [StringLength(20)]
        public string Name { get; set; }
        [StringLength(1000)]
        public String Description { get; set; }
        public string Formula { get; set; }
        public int? FormulaValue { get; set; }
        public string ScoreLike { get; set; }
        public bool? Discardable { get; set; }
        public bool? CameToStart { get; set; }
        public bool? Started { get; set; }
        public bool? Finished { get; set; }
        public bool? PreserveResult { get; set; }
        // Should scoring of other following competitors use this as a finisher ahead? 
        public bool? AdjustOtherScores { get; set; }

    }
}
