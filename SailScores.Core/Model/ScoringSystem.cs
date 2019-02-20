using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SailScores.Core.Model
{
    public class ScoringSystem
    {
        Guid Id { get; set; }
        Guid ClubId { get; set; }
        [StringLength(100)]
        String Name { get; set; }

        String DiscardPattern { get; set; }

        IList<ScoreCode> ScoreCodes { get; set; }

    }
}
