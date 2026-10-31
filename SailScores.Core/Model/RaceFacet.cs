using System;

namespace SailScores.Core.Model
{
    // The number of races for one fleet in one month.
    public class RaceFacet
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public Guid? FleetId { get; set; }
        public String FleetShortName { get; set; }
        public String FleetName { get; set; }
        public int Count { get; set; }
    }
}
