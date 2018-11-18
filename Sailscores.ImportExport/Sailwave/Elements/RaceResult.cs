using Sailscores.ImportExport.Sailwave.Attributes;

namespace Sailscores.ImportExport.Sailwave.Elements
{
    public class RaceResult
    {
        public int CompetitorId { get; set; }
        public int RaceId { get; set; }

        [SailwaveProperty("rdisc")]
        public bool? Discarded { get; set; }
        [SailwaveProperty("rlps")]
        public int? Laps { get; set; }
        [SailwaveProperty("rpts")]
        public decimal? Points { get; set; }
        [SailwaveProperty("rpos")]
        public int? Position { get; set; }
        [SailwaveProperty("rdisc")]
        public int? Disc { get; set; }
        [SailwaveProperty("rrecpos")]
        public int Place { get; set; }
        [SailwaveProperty("rrestyp")]
        public int ResultType { get; set; }
        [SailwaveProperty("srat")]
        public int Rating { get; set; }
        [SailwaveProperty("rrset")]
        public int Set { get; set; }
        [SailwaveProperty("rcod")]
        public string Code { get; set; }
    }
}