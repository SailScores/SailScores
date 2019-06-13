using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SailScores.Database.Entities
{
    public class ScoringSystem
    {
        public Guid Id { get; set; }
        public Guid? ClubId { get; set; }

        public Guid? ParentSystemId { get; set; }

        [StringLength(100)]
        public String Name { get; set; }

        public String DiscardPattern { get; set; }

        public IList<ScoreCode> ScoreCodes { get; set; }
        
        [ForeignKey("ParentSystemId")]
        public ScoringSystem ParentSystem { get; set; }

    }
}
