using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class ScoreViewModel
    {
        public Guid Id { get; set; }
        public Competitor Competitor { get; set; }
        public Guid CompetitorId { get; set; }
        public Race Race { get; set; }
        public Guid RaceId { get; set; }
        public int? Place { get; set; }
        public string Code { get; set; }

        // This is a best guess at the ScoreCode represented by this
        // score. Really the name of this matters more than the
        // actual object in this field.
        // For example, DNS could connect to two different ScoreCodes,
        // depending on the series for which the score is being
        // calculated. Only one of them would be here.
        public ScoreCode ScoreCode { get; set; }

        public decimal? CodePoints { get; set; }
        // used for parsing CodePoints, can use period or comma as decimal separator
        public string CodePointsString { get; set; }
    }
}
