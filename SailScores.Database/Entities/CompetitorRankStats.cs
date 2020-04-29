using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.Database.Entities
{
    public class CompetitorRankStats
    {
        public string SeasonName { get; set; }
        public DateTime SeasonStart { get; set; }
        public int? Place { get; set; }
        public string Code { get; set; }
        public int? Count { get; set; }
    }
}
