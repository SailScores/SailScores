using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores
{
    public class ScoringSystemCanBeDeletedViewModel : ScoringSystem
    {
        public bool InUse { get; set; }
    }
}
