using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class ScoreCode
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }
        public Guid ScoringSystemId { get; set; }
        [StringLength(20)]
        public String Name { get; set; }
        [StringLength(1000)]
        public String Description { get; set; }

        // can be:
        // COD - Use value of ScoreLike to find another code to use
        // FIN+ - competitors who finished this race + FormulaValue
        // SER+ - competitors in this series + FormulaValue
        // CTS+ - competitors who came to start + FormulaValue
        // AVE - average of all non-discarded races
        // PLC% - Place + xx% of DNF score (xx is stored FormulaValue)
        // MAN - allow scorer to enter score manually
        // TIE - Tied with previous finisher
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
