using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class ScoringSystem
    {
        public Guid Id { get; set; }
        public Guid? ClubId { get; set; }

        [Required]
        [StringLength(100)]
        public String Name { get; set; }

        [Required]
        public String DiscardPattern { get; set; }

        public IList<ScoreCode> ScoreCodes { get; set; }

        public Guid? ParentSystemId { get; set; }

        public ScoringSystem ParentSystem { get; set; }

        public IEnumerable<ScoreCode> InheritedScoreCodes { get; set; }

        [DisplayName("Participation Percent")]
        public Decimal? ParticipationPercent { get; set; }

    }
}
