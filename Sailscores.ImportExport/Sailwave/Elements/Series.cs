using System.Collections.Generic;

namespace Sailscores.ImportExport.Sailwave.Elements
{
    public class Series
    {
        public SeriesDetails Details { get; set; }
        public List<ScoringSystem> ScoringSystems { get; set; }
        public UserInterfaceInfo UserInterface { get; set; }
        public List<Competitor> Competitors { get; set; }
        public List<Race> Races { get; set; }
        public List<Column> Columns { get; set; }

    }
}
