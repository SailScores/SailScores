using SailScores.Core.FlatModel;
using SailScores.Core.Scoring;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores
{
    public class ScoreCellViewModel
    {
        public FlatCalculatedScore Result { get; set; } 
        public bool IsPercentSystem { get; set; } 
        public Dictionary<string, ScoreCodeSummary> ScoreCodesUsed { get; set; }
    }
}
