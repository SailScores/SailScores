using System.Collections.Generic;
using SailScores.ImportExport.Sailwave.Attributes;

namespace SailScores.ImportExport.Sailwave.Elements
{
    public class ScoringSystem
    {
        public int Id { get; set; }
        [SailwaveProperty("scrfollowchamp", SailwavePropertyType.OneZero)]
        public bool FollowChamp { get; set; }
        [SailwaveProperty("scrfollowmedal", SailwavePropertyType.OneZero)]
        public bool FollowMedal { get; set; }
        [SailwaveProperty("scrfollowcodes", SailwavePropertyType.OneZero)]
        public bool FollowCodes { get; set; }
        [SailwaveProperty("scrfollowratingsystem", SailwavePropertyType.OneZero)]
        public bool FollowRatingSystem { get; set; }
        [SailwaveProperty("scrfollowratingmode", SailwavePropertyType.OneZero)]
        public bool FollowRatingMode { get; set; }
        [SailwaveProperty("scrfollowpointssystem", SailwavePropertyType.OneZero)]
        public bool FollowPointsSystem { get; set; }
        [SailwaveProperty("scrfollowcoefficients", SailwavePropertyType.OneZero)]
        public bool FollowCoefficients { get; set; }
        [SailwaveProperty("scrfollowcustomexprs", SailwavePropertyType.OneZero)]
        public bool FollowCustomExpressions { get; set; }
        [SailwaveProperty("scrfollowseriesties", SailwavePropertyType.OneZero)]
        public bool FollowSeriesTies { get; set; }
        [SailwaveProperty("scrfollowoverallpoints", SailwavePropertyType.OneZero)]
        public bool FollowOverallPoints { get; set; }
        [SailwaveProperty("scrfollowdiscards", SailwavePropertyType.OneZero)]
        public bool FollowDiscards { get; set; }
        [SailwaveProperty("scrfollowraceties", SailwavePropertyType.OneZero)]
        public bool FollowRaceTies { get; set; }
        [SailwaveProperty("scrfollowflights", SailwavePropertyType.OneZero)]
        public bool FollowFlights { get; set; }
        [SailwaveProperty("scrfollowpointsplaces", SailwavePropertyType.OneZero)]
        public bool FollowPointsPlaces { get; set; }
        [SailwaveProperty("scrfollowqualification", SailwavePropertyType.OneZero)]
        public bool FollowQualification { get; set; }
        [SailwaveProperty("scrhotkey", SailwavePropertyType.OneZero)]
        public bool HotKey { get; set; }
        [SailwaveProperty("scrname")]
        public string Name { get; set; }
        [SailwaveProperty("scrparent")]
        public int Parent { get; set; }
        [SailwaveProperty("scrhighpointformula")]
        public string HighPointFormula { get; set; }
        [SailwaveProperty("scrsfield")]
        public string Field { get; set; }
        [SailwaveProperty("scrsfield2")]
        public string Field2 { get; set; }
        [SailwaveProperty("scrnotes")]
        public string Notes { get; set; }
        [SailwaveProperty("scrtie0")]
        public bool Tie0 { get; set; }
        [SailwaveProperty("scrtie1")]
        public bool Tie1 { get; set; }
        [SailwaveProperty("scrincludediscards")]
        public bool IncludeDiscards { get; set; }
        [SailwaveProperty("scrtie2")]
        public bool Tie2 { get; set; }
        [SailwaveProperty("scrtie2back")]
        public bool Tie2Back { get; set; }
        [SailwaveProperty("scrtie2starts")]
        public bool Tie2Starts { get; set; }
        [SailwaveProperty("scrtiefinals")]
        public bool TieFinals { get; set; }

        [SailwaveProperty("scrraceties")]
        public string RaceTies { get; set; } = "Averaged";

        [SailwaveProperty("scrpointsplaces")]
        public int PointsPlaces { get; set; } = 1;
        [SailwaveProperty("scrpointsaccumulation")]
        public string PointsAccumulation { get; set; }
        [SailwaveProperty("scrpointssystem")]
        public string PointsSystem { get; set; }
        [SailwaveProperty("scrratingsystem")]
        public string RatingSystem { get; set; }
        [SailwaveProperty("scrratingvalue")]
        public bool RatingValue { get; set; }
        [SailwaveProperty("scrwindstrengths")]
        public string WindStrengths { get; set; }
        [SailwaveProperty("scrupdateratings")]
        public bool UpdateRatings { get; set; }
        [SailwaveProperty("scrbackcalcpercent")]
        public int BackCalcPercent { get; set; }
        [SailwaveProperty("scrdiscardlist")]
        public string DiscardList { get; set; }
        [SailwaveProperty("scrflights")]
        public bool Flights { get; set; }
        [SailwaveProperty("scrfinals")]
        public bool Finals { get; set; }
        [SailwaveProperty("scrfinalsstickyq")]
        public bool FinalsStickyQ { get; set; }
        [SailwaveProperty("scrfinalsfirst")]
        public int FinalsFirst { get; set; }
        [SailwaveProperty("scrlowweight")]
        public bool LowWeight { get; set; }
        [SailwaveProperty("scrmedal")]
        public bool Medal { get; set; }
        [SailwaveProperty("scrmedalrace")]
        public int MedalRace { get; set; }
        [SailwaveProperty("scrmedalyesnotdiscardable")]
        public bool MedalYesNotDiscardable { get; set; }
        [SailwaveProperty("scrmedalyesmultiply")]
        public bool MedalYesMultiply { get; set; }
        [SailwaveProperty("scrmedalyestie")]
        public bool MedalYesTie { get; set; }
        [SailwaveProperty("scrmedalnonotdiscardable")]
        public bool MedalNoNotDiscardable { get; set; }
        [SailwaveProperty("scrmedalnomultiply")]
        public bool MedalNoMultiply { get; set; }
        [SailwaveProperty("scrmedalnotie")]
        public bool MedalNoTie { get; set; }
        [SailwaveProperty("scrmedalmax")]
        public int MedalMax { get; set; } = 10;
        public IList<ScoreCode> Codes { get; set; }

        //"scrcode","BFD|Score like|DNF|Yes|Yes||spare|spare|spare|spare|spare|No|No|No|5||Black flag disqualification under rule 30.3","",""
        //"scrcode","DGM|Score like|DSQ|No|Yes||spare|spare|spare|spare|spare|Yes|Yes|No|5||Disqualification not excludable under rule 69.1(b)(2)","",""
        //"scrcode","DNC|Boats in series +|1|Yes|No||spare|spare|spare|spare|spare|No|No|No|5||Did not come to the starting area","",""
        //"scrcode","DNE|Score like|DSQ|No|Yes||spare|spare|spare|spare|spare|Yes|Yes|No|5||Disqualification (other then DGM) not excludable under rule 88.3(b)","",""
        //"scrcode","DNF|Boats in series +|1|Yes|Yes||spare|spare|spare|spare|spare|Yes|No|No|5||Started but did not finish","",""
        //"scrcode","DNS|Score like|DNF|Yes|Yes||spare|spare|spare|spare|spare|No|No|No|5||Came to the start area but did not start","",""
        //"scrcode","DPI1|Fixed penalty|1|Yes|Yes||spare|spare|spare|spare|spare|Yes|Yes|Yes|5||1 point discretionary penalty","",""
        //"scrcode","DSQ|Score like|DNF|Yes|Yes||spare|spare|spare|spare|spare|Yes|Yes|No|5||Disqualification","",""
        //"scrcode","OCS|Score like|DNF|Yes|Yes||spare|spare|spare|spare|spare|No|No|No|5||On course side at start or broke rule 30.1","",""
        //"scrcode","OOD|Average (all)||Yes|No||spare|spare|spare|spare|spare|Yes|Yes|No|5||Race officer duty points scored as RDGa","",""
        //"scrcode","RDG|Set points by hand||Yes|Yes||spare|spare|spare|spare|spare|Yes|Yes|Yes|5||Redress - points set by protest hearing","",""
        //"scrcode","RDGa|Average (all)||Yes|Yes||spare|spare|spare|spare|spare|Yes|Yes|Yes|5||Redress - average points for all races except the race in question","",""
        //"scrcode","RDGb|Average (before)||Yes|Yes||spare|spare|spare|spare|spare|Yes|Yes|Yes|5||Redress - average points for all races preceeding the race in question","",""
        //"scrcode","RET|Score like|DNF|Yes|Yes||spare|spare|spare|spare|spare|Yes|No|No|5||Retired","",""
        //"scrcode","SCP|Percentage penalty|20|Yes|Yes||spare|spare|spare|spare|spare|Yes|Yes|Yes|5||Scoring penalty under rule 44.3","",""
        //"scrcode","ZFP|Percentage penalty|20|Yes|Yes||spare|spare|spare|spare|spare|Yes|Yes|Yes|5||20% penalty under rule 30.2","",""

    }
}