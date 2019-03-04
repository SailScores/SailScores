using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SailScores.Core.Model
{
    public class ScoringSystem
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }
        [StringLength(100)]
        public String Name { get; set; }

        public String DiscardPattern { get; set; }

        public IList<ScoreCode> ScoreCodes { get; set; }

    }
}
