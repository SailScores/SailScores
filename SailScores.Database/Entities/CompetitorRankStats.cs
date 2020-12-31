using System;

namespace SailScores.Database.Entities
{
    // Exists to support the return of SQL view results. Does
    // NOT need to be an actual table in db.
    public class CompetitorRankStats
    {
        public string SeasonName { get; set; }
        public DateTime SeasonStart { get; set; }
        public int? Place { get; set; }
        public string Code { get; set; }
        public int? Count { get; set; }
    }
}
