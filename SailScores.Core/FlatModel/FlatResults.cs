using SailScores.Api.Enumerations;
using SailScores.Core.Scoring;
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
        // Extra columns after the total (see ScoringColumns); null for results saved
        // before these existed until ScoringColumns.AddLegacyColumns fills them in.
        public IList<FlatResultColumn> Columns { get; set; }
        // Heading of the total column, from the calculator; null (older results) means "Total".
        public string TotalHeader { get; set; }
        // From the calculator: an unranked competitor's total column shows PointsEarned, not Average.
        public bool UnrankedShowsPoints { get; set; }
        public int NumberOfDiscards { get; set; }
        // Null for results saved before this was recorded; those used the race-count pattern.
        public DiscardMethod? DiscardMethod { get; set; }
        public int NumberOfSailedRaces { get; set; }
        public bool? IsPreliminary { get; set; }
        public bool IsPercentSystem { get; set; }
        public bool? LowerScoreWins { get; set; }
        public decimal? PercentRequired { get; set; }
        public string ScoringSystemName { get; set; }

        public String UpdatedBy { get; set; }
        public Dictionary<string, ScoreCodeSummary> ScoreCodesUsed { get; set; }
        public IList<AppliedScoreCodeGroupSummary> AppliedScoreCodeGroupSummaries { get; set; }

        public FlatSeriesScore GetScore(FlatCompetitor comp)
        {
            return CalculatedScores.FirstOrDefault(s => s.CompetitorId == comp.Id);
        }

        public FlatCalculatedScore GetScore(FlatCompetitor comp, FlatRace race)
        {
            return GetScore(comp?.Id ?? Guid.Empty, race?.Id ?? Guid.Empty);
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
