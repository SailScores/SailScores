namespace SailScores.ImportExport.Sailwave.Elements
{
    public class ScoreCode
    {
        public string Code { get; set; }
        public string Method { get; set; }
        public string Value { get; set; }
        public bool Discardable { get; set; }
        public bool CameToStartArea { get; set; }
        public bool Started { get; set; }
        public bool Finished { get; set; }
        public bool RuleA6d2Applies { get; set; }
        public int ScoringSystemId { get; set; }
        public string Format { get; set; }
        public string Description { get; set; }
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