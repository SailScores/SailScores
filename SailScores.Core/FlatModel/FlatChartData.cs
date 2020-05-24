using System.Collections.Generic;

namespace SailScores.Core.FlatModel
{
    public class FlatChartData
    {
        public IEnumerable<FlatCompetitor> Competitors { get; set; }
        public IEnumerable<FlatRace> Races { get; set; }
        public IEnumerable<FlatChartPoint> Entries { get; set; }

        public bool IsLowPoints { get; set; }
    }
}
