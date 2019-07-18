using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit
{


    public class LakeHarrietCalculatorTests
    {

        private AppendixACalculator _defaultCalculator;

        public LakeHarrietCalculatorTests()
        {
            _defaultCalculator = new AppendixACalculator(MakeDefaultScoringSystem());
        }

        private ScoringSystem MakeDefaultScoringSystem()
        {
            var system = new ScoringSystem
            {
                Name = "Default scoring system",
                DiscardPattern = "0,0,0,0,1,2,2,2,3,3,3,4,4,4,5,5,5,6,6," +
                    "6,7,7,7,8,8,8,9,9,9,10,10,10,11,11,11,12,12,12,13,13,13"
            };

            system.ScoreCodes = new List<ScoreCode>
            {
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "DNC",
                    PreserveResult = false,
                    Discardable = true,
                    Started = false,
                    FormulaValue = 2,
                    AdjustOtherScores = null,
                    CameToStart = false,
                    Finished = false,
                    Formula = "FIN+",
                    ScoreLike = null
                },
                                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "SB",
                    PreserveResult = false,
                    Discardable = true,
                    Started = false,
                    FormulaValue = null,
                    AdjustOtherScores = null,
                    CameToStart = false,
                    Finished = false,
                    Formula = "AVE ND",
                    ScoreLike = null
                },
            };

            return system;
        }

        [Fact]
        public void CalculateResults_ValidSeries_ReturnsResults()
        {
            var results = _defaultCalculator.CalculateResults(GetBasicSeries(3,3));

            Assert.NotNull(results);
        }

        [Fact]
        public void CalculateResults_3Races_NoDiscards()
        {
            var results = _defaultCalculator.CalculateResults(GetBasicSeries(3, 3));

            Assert.True(results.Results.All(r => r.Value.CalculatedScores.All(c => !c.Value.Discard)));
        }

        [Fact]
        public void CalculateResults_SafetyBoat_GetsAValue()
        {
            // Arrange: put in some coded results: SB
            var basicSeries = GetBasicSeries(3, 3);
            basicSeries.Races.First().Scores.First().Code = "SB";
            basicSeries.Races.First().Scores.First().Place = null;
            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.True(results.Results.First().Value.CalculatedScores.First().Value.RawScore.Place !=
                results.Results.First().Value.CalculatedScores.First().Value.ScoreValue);
        }

        [Fact]
        public void CalculateResults_SafetyBoat_GetsAverageOfTwoRacesValue()
        {
            // Arrange: put in some coded results: SB
            var basicSeries = GetBasicSeries(3, 6);
            var testComp = basicSeries.Competitors.First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == testComp).Code = "SB";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == testComp).Place = null;

            basicSeries.Races[basicSeries.Races.Count - 2].Scores.First(s => s.Competitor == testComp).Code = "SB";
            basicSeries.Races[basicSeries.Races.Count - 2].Scores.First(s => s.Competitor == testComp).Place = null;

            basicSeries.Races[1].Scores.First(s => s.Competitor == testComp).Place = 2;
            basicSeries.Races[1].Scores.First(s => s.Competitor != testComp).Place = 1;

            basicSeries.Races[2].Scores.First(s => s.Competitor == testComp).Place = 3;
            basicSeries.Races[2].Scores.Last().Place = 1;

            basicSeries.Races[3].Scores.First(s => s.Competitor == testComp).Place = 3;
            basicSeries.Races[3].Scores.Last().Place = 1;

            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.Equal(1.5m,
                results.Results[testComp].CalculatedScores.Last().Value.ScoreValue);
        }


        [Fact]
        public void CalculateResults_Dnc_GetsRaceCmopetitorsPlusTwo()
        {
            // Arrange: put in some coded results: SB
            var basicSeries = GetBasicSeries(4, 6);
            var testComp = basicSeries.Competitors.First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == testComp).Code = "DNC";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == testComp).Place = null;

            basicSeries.Races[basicSeries.Races.Count - 2].Scores.First(s => s.Competitor == testComp).Code = "DNC";
            basicSeries.Races[basicSeries.Races.Count - 2].Scores.First(s => s.Competitor == testComp).Place = null;

            basicSeries.Races[1].Scores.First(s => s.Competitor == testComp).Place = 2;
            basicSeries.Races[1].Scores.First(s => s.Competitor != testComp).Place = 1;

            basicSeries.Races[2].Scores.First(s => s.Competitor == testComp).Place = 3;
            basicSeries.Races[2].Scores.Last().Place = 1;

            basicSeries.Races[3].Scores.First(s => s.Competitor == testComp).Place = 3;
            basicSeries.Races[3].Scores.Last().Place = 1;

            var results = _defaultCalculator.CalculateResults(basicSeries);

            // three sailed in the race plus 2 = 5
            Assert.Equal(5m,
                results.Results[testComp].CalculatedScores.Last().Value.ScoreValue);
        }


        private Series GetBasicSeries(
            int competitorCount,
            int raceCount)
        {
            var competitors = new List<Competitor>();
            for (int i = 0; i < competitorCount; i++)
            {
                competitors.Add(
                    new Competitor
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Competitor {i}"
                    });
            }
            var races = new List<Race>();
            for (int i = 0; i < raceCount; i++)
            {
                var tmpRace =
                    new Race
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Race {i}",

                    };
                var scores = new List<Score>();
                for( int j=0; j < competitors.Count; j++)
                {
                    scores.Add(new Score
                    {
                        Competitor = competitors[j],
                        Race = tmpRace,
                        Place = j + 1

                    });
                }
                tmpRace.Scores = scores;
                races.Add(tmpRace);

            }

            return new Series
            {
                Id = Guid.NewGuid(),
                ClubId = Guid.NewGuid(),
                Name = "Test Series",
                Description = "Test Series Description",
                Races = races,
                Competitors = competitors,
                Season = new Season
                {

                },
                Results = null

            };
        }
    }
}
