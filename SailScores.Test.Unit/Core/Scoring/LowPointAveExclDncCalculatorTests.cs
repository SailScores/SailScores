using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring
{
    public class LowPointAveExclDncCalculatorTests
    {
        private LowPointAveExclDncCalculator _calculator;

        private ScoringSystem MakeScoringSystem()
        {
            var system = new ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Low Point Ave Excl DNC",
                DiscardPattern = "0,0",
                ParentSystemId = null
            };

            system.InheritedScoreCodes = new List<ScoreCode>();
            system.ScoreCodes = new List<ScoreCode>
            {
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "DNC",
                    Description = "Did not come to starting area",
                    PreserveResult = false,
                    Discardable = true,
                    Started = false,
                    FormulaValue = 1,
                    AdjustOtherScores = null,
                    CameToStart = false,
                    Finished = false,
                    Formula = "SER+",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "RDGAve",
                    Description = "Redress: average of other races",
                    PreserveResult = true,
                    Discardable = true,
                    Started = true,
                    FormulaValue = null,
                    AdjustOtherScores = false,
                    CameToStart = true,
                    Finished = true,
                    Formula = "AVE",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                }
            };
            return system;
        }

        private Series GetSeriesWithParticipation(int competitorCount, int raceCount, int particapatedInAll)
        {
            var competitors = new List<Competitor>();
            for (int i = 0; i < competitorCount; i++)
            {
                competitors.Add(new Competitor { Id = Guid.NewGuid(), Name = $"Competitor {i}" });
            }
            var races = new List<Race>();
            for (int i = 0; i < raceCount; i++)
            {
                var race = new Race
                {
                    Id = Guid.NewGuid(),
                    Name = $"Race {i}",
                    Order = i + 1,
                    Date = DateTime.UtcNow.AddDays(i)
                };
                var scores = new List<Score>();
                for (int j = 0; j < competitors.Count; j++)
                {
                    // Only first minParticipation competitors participate in all races, others get DNC
                    scores.Add(new Score
                    {
                        Competitor = competitors[j],
                        Race = race,
                        Place = (j < particapatedInAll) ? j + 1 : null,
                        Code = (j < particapatedInAll) ? null : "DNC"
                    });
                }
                race.Scores = scores;
                races.Add(race);
            }
            return new Series
            {
                Id = Guid.NewGuid(),
                Name = "Test Series",
                Races = races,
                Competitors = competitors,
                ScoringSystem = MakeScoringSystem(),
                Results = null
            };
        }

        [Fact]
        public void Competitors_BelowParticipationRequirement_AreNotRanked()
        {
            // Arrange: Only first competitor participates in all races, others are DNC
            var series = GetSeriesWithParticipation(3, 5, 1);
            _calculator = new LowPointAveExclDncCalculator(series.ScoringSystem);

            // Act
            var results = _calculator.CalculateResults(series);

            // Assert
            var ranked = results.Results.Where(r => r.Value.Rank > 0).ToList();
            Assert.Single(ranked); // Only one competitor ranked
            Assert.Equal("Competitor 0", ranked[0].Key.Name);
        }

        [Fact]
        public void DncResults_AreNotIncludedInAverageOrTotal()
        {
            // Arrange: Competitor 0 has 2 finishes and 3 DNCs
            var series = GetSeriesWithParticipation(2, 5, 2);
            var comp = series.Competitors.First();
            // Set up: first 3 races are finishes (1st, 2nd, 3rd.), last 2 are DNC
            for (int i = 0; i < 5; i++)
            {
                var score = series.Races[i].Scores.First();
                if (i >= 2)
                {
                    // This creates a tie in the second race, so remove that later
                    score.Place = i + 1;
                    score.Code = null;
                }
                else
                {
                    score.Place = null;
                    score.Code = "DNC";
                }
            }
            var secondComp = series.Competitors.Skip(1).First();
            for (int i = 0; i < 5; i++)
            {
                var score = series.Races[i].Scores.First();
                if (i == 2)
                {
                    // Clear up the tie from above
                    score.Place = 1;
                    score.Code = null;
                }
                else
                {
                    score.Place = 2;
                }
            }
            _calculator = new LowPointAveExclDncCalculator(series.ScoringSystem);

            // Act
            var results = _calculator.CalculateResults(series);
            var compResults = results.Results[comp];
            var total = compResults.TotalScore;

            // Assert: Three races should count for total and average
            Assert.Equal(2.0m, total); // (1 + 2 +3) / 3
        }
    }
}
