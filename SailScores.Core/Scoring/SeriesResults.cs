using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{
    public class SeriesResults
    {
        public IList<Race> Races { get; set; }
        public IList<Competitor> Competitors { get; set; }

        public Dictionary<Competitor, int?> Places { get; set; }

        public int NumberOfDiscards { get; set; }

        public bool IsPercentSystem { get; set; }

        public Decimal? PercentRequired { get; set; }

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

        public int GetSailedRaceCount()
        {
            return Races?.Count(r =>
            (r.State ?? RaceState.Raced) == RaceState.Raced
            || r.State == RaceState.Preliminary)
                ?? 0;
        }

        public IEnumerable<Race> SailedRaces
        {
            get
            {
                return Races?.
                    Where(r => (r?.State ?? RaceState.Raced) == RaceState.Raced
                        || r.State == RaceState.Preliminary)
                    ?? new List<Race>();
            }
        }
    }
}
