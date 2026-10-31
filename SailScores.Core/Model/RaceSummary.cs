using SailScores.Api.Enumerations;
using System;
using System.Collections.Generic;

namespace SailScores.Core.Model
{
    // Only the race fields needed to list races: much lighter than a full Race.
    public class RaceSummary
    {
        public Guid Id { get; set; }
        public String Name { get; set; }
        public DateTime? Date { get; set; }
        public int Order { get; set; }
        public String Description { get; set; }
        public RaceState? State { get; set; }
        public String FleetName { get; set; }
        public String FleetShortName { get; set; }
        public IList<RaceSummaryScore> Scores { get; set; }
        public IList<RaceSummarySeries> Series { get; set; }
    }

    public class RaceSummaryScore
    {
        public int? Place { get; set; }
        public String Code { get; set; }
    }

    public class RaceSummarySeries
    {
        public String UrlName { get; set; }
        public String Name { get; set; }
    }
}
