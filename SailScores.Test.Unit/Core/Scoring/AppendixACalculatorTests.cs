using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit
{


    public class AppendixACalculatorTests
    {

        private AppendixACalculator _defaultCalculator;

        public AppendixACalculatorTests()
        {
            _defaultCalculator = new AppendixACalculator(MakeDefaultScoringSystem());
        }

        private ScoringSystem MakeDefaultScoringSystem()
        {
            var system = new ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Appendix A Low Point",
                DiscardPattern = "0,1",
                ParentSystemId = null
            };

            system.ScoreCodes = new List<ScoreCode>
            {
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "TIE",
                    Description = "Tied Result",
                    PreserveResult = true,
                    Discardable = true,
                    Started = true,
                    FormulaValue = null,
                    AdjustOtherScores = false,
                    CameToStart = true,
                    Finished = true,
                    Formula = "MAN",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "SCP",
                    Description = "Scoring Penalty Rule 44.3",
                    PreserveResult = true,
                    Discardable = true,
                    Started = true,
                    FormulaValue = 20,
                    AdjustOtherScores = false,
                    CameToStart = true,
                    Finished = true,
                    Formula = "PLC%",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "DNS",
                    Description = "Came to start area but did not start",
                    PreserveResult = false,
                    Discardable = true,
                    Started = false,
                    FormulaValue = 1,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = false,
                    Formula = "CTS+",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "RET",
                    Description = "Retired",
                    PreserveResult = true,
                    Discardable = true,
                    Started = true,
                    FormulaValue = 1,
                    AdjustOtherScores = true,
                    CameToStart = true,
                    Finished = true,
                    Formula = "CTS+",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "ZFP",
                    Description = "20% Penalty under rule 30.2",
                    PreserveResult = true,
                    Discardable = true,
                    Started = true,
                    FormulaValue = 20,
                    AdjustOtherScores = false,
                    CameToStart = true,
                    Finished = true,
                    Formula = "PLC%",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "DPI",
                    Description = "Discretionary Penalty",
                    PreserveResult = true,
                    Discardable = true,
                    Started = true,
                    FormulaValue = 20,
                    AdjustOtherScores = false,
                    CameToStart = true,
                    Finished = true,
                    Formula = "PLC%",
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
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "UFD",
                    Description = "Disqualification under rule 30.3",
                    PreserveResult = false,
                    Discardable = true,
                    Started = false,
                    FormulaValue = 1,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = false,
                    Formula = "CTS+",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "BFD",
                    Description = "Disqualification under rule 30.4",
                    PreserveResult = false,
                    Discardable = false,
                    Started = false,
                    FormulaValue = 1,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = false,
                    Formula = "CTS+",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "DNF",
                    Description = "Started but did not finish",
                    PreserveResult = false,
                    Discardable = true,
                    Started = true,
                    FormulaValue = 1,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = false,
                    Formula = "CTS+",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "OCS",
                    Description = "On course side as start or broke rule 30.1",
                    PreserveResult = false,
                    Discardable = true,
                    Started = false,
                    FormulaValue = 1,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = false,
                    Formula = "CTS+",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "DSQ",
                    Description = "Disqualification",
                    PreserveResult = false,
                    Discardable = true,
                    Started = true,
                    FormulaValue = 1,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = true,
                    Formula = "CTS+",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "RDG",
                    Description = "Redress: points set by protest hearing",
                    PreserveResult = true,
                    Discardable = true,
                    Started = true,
                    FormulaValue = null,
                    AdjustOtherScores = false,
                    CameToStart = true,
                    Finished = true,
                    Formula = "MAN",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "DNE",
                    Description = "Disqualification that is not excludable",
                    PreserveResult = false,
                    Discardable = false,
                    Started = true,
                    FormulaValue = 1,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = true,
                    Formula = "CTS+",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
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
        public void CalculateResults_3Races_OneDiscard()
        {
            var results = _defaultCalculator.CalculateResults(GetBasicSeries(3, 3));

            var firstCompetitor = results.Competitors.First();
            Assert.Equal(1, results.Results[firstCompetitor].CalculatedScores.Count(r => r.Value.Discard));
        }


        [Fact]
        public void CalculateResults_SafetyBoat_GetsDnc()
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

            Assert.Equal(4m,
                results.Results[testComp].CalculatedScores.Last().Value.ScoreValue);
        }

        [Fact]
        public void CalculateResults_DNC_GetsSeriesCompetitorsPlusOne()
        {
            // Arrange: put in some coded results: SB
            var basicSeries = GetBasicSeries(3, 6);
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

            Assert.Equal(4m,
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
