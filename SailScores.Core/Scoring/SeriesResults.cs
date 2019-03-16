using SailScores.Core.Model;
using System;
using System.Collections.Generic;

namespace SailScores.Core.Scoring
{
    public class SeriesResults
    {
        public IList<Race> Races { get; set; }
        public IList<Competitor> Competitors { get; set; }

        public Dictionary<Competitor, int?> Places { get; set; }
        
        public int NumberOfDiscards { get; set; }

        public Dictionary<Competitor, SeriesCompetitorResults> Results { get; set; }

        public CalculatedScore GetResult(
            Competitor comp,
            Race race)
        {
            if (Results.ContainsKey(comp) && Results[comp].CalculatedScores.ContainsKey(race))
            {
                var score = Results[comp]?.CalculatedScores[race];

                return score;
            }
            return new CalculatedScore
            {
                RawScore = new Score
                {
                    Code = "DNC",
                    Competitor = comp,
                    Race = race
                },
                Discard = false
            };
        }
    }
}
