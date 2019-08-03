using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Database.Entities
{
    public class ScoreCode
    {
        public Guid Id { get; set; }
        public Guid ScoringSystemId { get; set; }
        [StringLength(20)]
        public String Name { get; set; }
        [StringLength(1000)]
        public String Description { get; set; }
        [StringLength(100)]
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
