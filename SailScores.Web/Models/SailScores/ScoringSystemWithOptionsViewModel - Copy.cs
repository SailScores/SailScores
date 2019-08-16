using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores
{
    public class ScoringSystemWithOptionsViewModel : ScoringSystem
    {
        public IList<ScoringSystem> ParentSystemOptions { get; set; }
        public IList<ScoreCode> ScoreCodeOptions { get; set; }
    }
}