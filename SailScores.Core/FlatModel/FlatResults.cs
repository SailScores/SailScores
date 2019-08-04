using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//This namespace is likely to get moved to Api project
namespace SailScores.Core.FlatModel
{
    public class FlatResults
    {
        public Guid SeriesId { get; set; }

        public IEnumerable<FlatCompetitor> Competitors { get; set; }
        public IEnumerable<FlatRace> Races { get; set; }
        public IEnumerable<FlatSeriesScore> CalculatedScores { get; set; }
        public int NumberOfDiscards { get; set; }
        public int NumberOfSailedRaces { get; set; }
        public bool IsPercentSystem { get; set; }
        public decimal? PercentRequired { get; set; }
        public string ScoringSystemName { get; set; }
        public FlatSeriesScore GetScore(FlatCompetitor comp)
        {
            return CalculatedScores.FirstOrDefault(s => s.CompetitorId == comp.Id);
        }

        public FlatCalculatedScore GetScore(FlatCompetitor comp, FlatRace race)
        {
            return CalculatedScores
                .FirstOrDefault(s => s.CompetitorId == comp.Id)
                ?.Scores
                ?.FirstOrDefault(s => s.RaceId == race.Id);
        }
    }
}
