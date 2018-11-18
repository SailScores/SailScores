using System.Collections.Generic;
using Sailscores.ImportExport.Sailwave.Attributes;

namespace Sailscores.ImportExport.Sailwave.Elements
{
    public class Race
    {
        public int Id { get; set; }

        [SailwaveProperty("racerank")]
        public int Rank { get; set; }
        [SailwaveProperty("racesailed")]
        public bool Sailed { get; set; }
        [SailwaveProperty("racename")]
        public string Name { get; set; }
        [SailwaveProperty("racestart")]
        public string Start { get; set; } = "||Place|Start 1|||0||0|0||||1";

        public IEnumerable<RaceResult> Results { get; set; }
    }
}