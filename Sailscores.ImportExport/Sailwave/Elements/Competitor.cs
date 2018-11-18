using Sailscores.ImportExport.Sailwave.Attributes;

namespace Sailscores.ImportExport.Sailwave.Elements
{
    public class Competitor
    {
        public int Id { get; set; }

        [SailwaveProperty("comptally")]
        public int? Tally { get; set; }
        [SailwaveProperty("comptotal")]
        public decimal? TotalScore { get; set; }
        [SailwaveProperty("compnett")]
        public decimal? NetScore { get; set; }
        [SailwaveProperty("compboat")]
        public string Boat { get; set; }
        [SailwaveProperty("comprank")]
        public int? Rank { get; set; }
        [SailwaveProperty("compsailno")]
        public string SailNumber { get; set; }
        [SailwaveProperty("compclass")]
        public string Class { get; set; }
        [SailwaveProperty("compclub")]
        public string Club { get; set; }
        [SailwaveProperty("compnotes")]
        public string Notes { get; set; }
        [SailwaveProperty("compexclude")]
        public bool Exclude { get; set; } = false;
        [SailwaveProperty("compalias")]
        public string Alias { get; set; }
        [SailwaveProperty("comprating")]
        public decimal? Rating { get; set; }
        [SailwaveProperty("compwindrats")]
        public string WindRatings { get; set; }
        [SailwaveProperty("compstatus")]
        public string Status { get; set; }
        [SailwaveProperty("comppaid")]
        public string Paid { get; set; }
        [SailwaveProperty("compmedicalflag")]
        public string MedicalFlag { get; set; }
        [SailwaveProperty("comphelmname")]
        public string HelmName { get; set; }
        [SailwaveProperty("comphigh")]
        public int High { get; set; }
        
        // Going to try omitting these:
        //"compmedicalflag","0","57",""
        
    }
}