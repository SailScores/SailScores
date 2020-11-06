using System;
using System.Collections.Generic;
using System.Linq;

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
        public bool? IsPreliminary { get; set; }
        public bool IsPercentSystem { get; set; }
        public decimal? PercentRequired { get; set; }
        public string ScoringSystemName { get; set; }

        public String UpdatedBy { get; set; }
        public FlatSeriesScore GetScore(FlatCompetitor comp)
        {
            return CalculatedScores.FirstOrDefault(s => s.CompetitorId == comp.Id);
        }

        public FlatCalculatedScore GetScore(FlatCompetitor comp, FlatRace race)
        {
            return GetScore(comp?.Id ?? default, race?.Id ?? default);
        }
        public FlatCalculatedScore GetScore(Guid compId, Guid raceId)
        {
            return CalculatedScores
                .FirstOrDefault(s => s.CompetitorId == compId)
                ?.Scores
                ?.FirstOrDefault(s => s.RaceId == raceId);
        }
    }
}
