using Sailscores.Core.Model;
using Sailscores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Sailscores.Test.Unit
{


    public class SeriesCalculatorTests
    {

        private SeriesCalculator _calculator;

        public SeriesCalculatorTests()
        {
            _calculator = new SeriesCalculator();
        }

        [Fact]
        public void CalculateResults_ValidSeries_ReturnsResults()
        {
            var results = _calculator.CalculateResults(GetBasicSeries(3,3));

            Assert.NotNull(results);
        }

        [Fact]
        public void CalculateResults_3Races_NoDiscards()
        {
            var results = _calculator.CalculateResults(GetBasicSeries(3, 3));

            Assert.True(results.Results.All(r => r.Value.CalculatedScores.All(c => !c.Value.Discard)));
        }

        [Fact]
        public void CalculateResults_SafetyBoat_GetsAValue()
        {
            // Arrange: put in some coded results: SB
            var basicSeries = GetBasicSeries(3, 3);
            basicSeries.Races.First().Scores.First().Code = "SB";
            basicSeries.Races.First().Scores.First().Place = null;
            var results = _calculator.CalculateResults(basicSeries);

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

            var results = _calculator.CalculateResults(basicSeries);

            Assert.Equal(1.5m,
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
