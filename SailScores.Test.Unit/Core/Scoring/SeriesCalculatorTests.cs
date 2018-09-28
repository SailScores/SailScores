using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using Xunit;

namespace SailScores.Test.Unit
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
                        Place = j

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
                Season = new Season
                {

                },
                Results = null

            };
        }
    }
}
