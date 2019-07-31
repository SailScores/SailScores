using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit
{

    public class HighPointPercCalculatorTests
    {

        private HighPointPercentageCalculator _defaultCalculator;

        public HighPointPercCalculatorTests()
        {
            _defaultCalculator = new HighPointPercentageCalculator(MakeDefaultScoringSystem());
        }

        private ScoringSystem MakeDefaultScoringSystem()
        {
            var system = new ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "High Point Percentage For Series",
                DiscardPattern = "0,1",
                ParentSystemId = null,
                ParticipationPercent = 60m
            };

            system.InheritedScoreCodes = new List<ScoreCode>();

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
                    Formula = "TIE",
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
                    FormulaValue = 0,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = false,
                    Formula = "FIX",
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
                    FormulaValue = 0,
                    AdjustOtherScores = true,
                    CameToStart = true,
                    Finished = true,
                    Formula = "FIX",
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
                    FormulaValue = 0,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = false,
                    Formula = "FIX",
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
                    FormulaValue = 0,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = false,
                    Formula = "FIX",
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
                    FormulaValue = 0,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = false,
                    Formula = "FIX",
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
                    FormulaValue = 0,
                    AdjustOtherScores = true,
                    CameToStart = true,
                    Finished = false,
                    Formula = "FIX",
                    ScoreLike = null,
                    ScoringSystemId = system.Id
                },
                new ScoreCode
                {
                    Id = Guid.NewGuid(),
                    Name = "DSQ",
                    Description = "Disqualification",
                    PreserveResult = true,
                    Discardable = true,
                    Started = true,
                    FormulaValue = 0,
                    AdjustOtherScores = false,
                    CameToStart = true,
                    Finished = true,
                    Formula = "FIX",
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
                    FormulaValue = 0,
                    AdjustOtherScores = null,
                    CameToStart = true,
                    Finished = true,
                    Formula = "FIX",
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
                    FormulaValue = 0,
                    AdjustOtherScores = null,
                    CameToStart = false,
                    Finished = false,
                    Formula = "FIX",
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
        public void CalculateResults_5Races_OneDiscard()
        {
            var results = _defaultCalculator.CalculateResults(GetBasicSeries(4, 5));

            var firstCompetitor = results.Competitors.First();
            Assert.Equal(1, results.Results[firstCompetitor].CalculatedScores.Count(r => r.Value.Discard));
        }

        [Fact]
        public void CalculateResults_DNE_IsNotExcluded()
        {
            var basicSeries = GetBasicSeries(10, 6);
            var testComp = basicSeries.Competitors.First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == testComp).Code = "DNE";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == testComp).Place = null;


            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.NotEqual(100m,
                results.Results[testComp].TotalScore);
            Assert.True(
                results.Results[testComp].Rank > 1);
        }

        // This is a test of what happens to an undefined code: SB is not defined in the base system,
        // so the default (DNC) should be used instead. 
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

            Assert.Equal(0m,
                results.Results[testComp].CalculatedScores.Last().Value.ScoreValue);
        }

        [Fact]
        public void CalculateResults_DNC_GetsZero()
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

            Assert.Equal(0m,
                results.Results[testComp].CalculatedScores.Last().Value.ScoreValue);
        }

        [Fact]
        public void CalculateResults_DNF_GetsZero()
        {
            // Arrange: put in some coded results: SB
            var basicSeries = GetBasicSeries(3, 6);
            var testComp = basicSeries.Competitors.First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == testComp).Code = "DNF";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == testComp).Place = null;

            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.Equal(0m,
                results.Results[testComp].CalculatedScores.Last().Value.ScoreValue);
        }

        [Fact]
        public void CalculateResults_DSQ_GetsZero()
        {
            // Arrange: put in some coded results: SB
            var basicSeries = GetBasicSeries(3, 6);
            var testComp = basicSeries.Competitors.First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == testComp).Code = "DSQ";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == testComp).Place = null;

            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.Equal(0m,
                results.Results[testComp].CalculatedScores.Last().Value.ScoreValue);
        }



        [Fact]
        public void CalculateResults_Penalty_GetsDnf()
        {
            var basicSeries = GetBasicSeries(20, 1);

            var lastComp = basicSeries.Competitors.Skip(19).First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == lastComp).Code = "DPI";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == lastComp).Place = 20;

            var nextToLastComp = basicSeries.Competitors.Skip(18).First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == nextToLastComp).Code = "DNF";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == nextToLastComp).Place = null;

            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.Equal(
                results.Results[nextToLastComp].CalculatedScores.Last().Value.ScoreValue,
                results.Results[lastComp].CalculatedScores.Last().Value.ScoreValue);
        }

        // https://www.rya.org.uk/SiteCollectionDocuments/Racing/RacingInformation/RaceOfficials/Resource%20Centre/Best%20Practice%20Guidelines%20Policies/Scoring.pdf

        // Example 1: 23 boats entered the series. Boat A finishes 3rd in the race but is ZFP. The penalty is 20% of 23 = 4.6 places,
        // rounded to 5 places so she receives points for the place equal to her finishing place of 3rd plus 5 penalty places - 8th place.
        // Under the Low Point System, 8th place receives 8 points so points for the race are: 1, 2, 4, 5, 6, 7, 8, [8], 9, 10 … 23. (The
        // boxed number is A’s score.) The two boats scoring 8 points will share any race prize for 7th place; the boat scoring 9 points will
        // receive any race prize for 9th place.Remember that under rule 44.3 (and therefore under rule 30.2) a boat shall not receive a
        // score that is worse than DNF would receive.A DNF score in this race would be 24 (23 series entrants, plus 1), which would be
        // the penalty for a ZFP boat with a finishing position of 20th or worse.
        [Fact]
        public void CalculateResults_ZFP_ThirdOf23GetsEighthPlace()
        {
            var basicSeries = GetBasicSeries(23, 1);
            var thirdComp = basicSeries.Competitors.Skip(2).First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == thirdComp).Code = "ZFP";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == thirdComp).Place = 3;

            var results = _defaultCalculator.CalculateResults(basicSeries);

            // third is 21 penalty is 5
            Assert.Equal(16m,
                results.Results[thirdComp].CalculatedScores.Last().Value.ScoreValue);

            //tied for seventh place
            Assert.Equal(7, results.Results[thirdComp].Rank);
        }

        [Fact]
        public void CalculateResults_ZFP_20Of23Gets0()
        {
            var basicSeries = GetBasicSeries(23, 1);
            var twentiethComp = basicSeries.Competitors.Skip(19).First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == twentiethComp).Code = "ZFP";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == twentiethComp).Place = 20;

            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.Equal(0m,
                results.Results[twentiethComp].CalculatedScores.Last().Value.ScoreValue);
        }

        // https://www.rya.org.uk/SiteCollectionDocuments/Racing/RacingInformation/RaceOfficials/Resource%20Centre/Best%20Practice%20Guidelines%20Policies/Scoring.pdf
        //Scoring penalties under rules 30.2 and/or 44.3 are cumulative but are calculated individually.For example, if a boat breaks rule
        //30.2 and the race is recalled and she again breaks rule 30.2 in the restart, she will have two 20% penalties.Similarly, if she
        //breaks 30.2 and also takes a Scoring Penalty under rule 44.3 (SCP) she will have two 20% penalties (assuming the sailing
        //instructions do not specify that the Scoring Penalty will be other than 20%).
        //Example 2: Same as Example 1 above except that boat A also takes a 20% SCP under rule 44.3. She receives two penalties
        //of 5 places each for a total of 10 places(not a 40% penalty of 9.2 places rounded to 9 places). Her score would be the score for
        //13th place, namely her finishing place of 3rd plus 10 penalty places.Points for the race are: 1, 2, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13,
        //13, 14, 15… 

        // SailScores does not currently have a way to indicate multiple penalties: score codes are assigned one per result. The current work-around is manually scoring a result.
        // putting the test in here with an ignore as a note for future desireability.

        [Fact(Skip ="Not implemented. See test comment")]
        public void CalculateResults_MultiplePenalties_CumulativeImpact()
        {
            var basicSeries = GetBasicSeries(23, 1);
            var thirdComp = basicSeries.Competitors.Skip(2).First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == thirdComp).Code = "ZFP";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == thirdComp).Place = 3;

            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.Equal(13m,
                results.Results[thirdComp].CalculatedScores.Last().Value.ScoreValue);
        }

        // In appendix A, DSQ pulls scores after that up. In the reference for the High Point percent system I used, the opposite is the case:
        // 
        // https://www.ussailing.org/competition/rules-officiating/racing-rules/scoring-a-long-series/
        // 
        [Fact]
        public void CalculateResults_Dsq_NotIgnoredForPenaltyAfter()
        {
            var basicSeries = GetBasicSeries(23, 1);
            var thirdComp = basicSeries.Competitors.Skip(2).First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == thirdComp).Code = "ZFP";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == thirdComp).Place = 3;

            var secondComp = basicSeries.Competitors.Skip(1).First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == secondComp).Code = "DSQ";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == secondComp).Place = 2;

            var results = _defaultCalculator.CalculateResults(basicSeries);

            // 20% scoring penalty in fleet is minus 5
            Assert.Equal(21m - 5m,
                results.Results[thirdComp].CalculatedScores.Last().Value.ScoreValue);
        }

        [Fact]
        public void CalculateResults_Dsq_GetsZero()
        {
            var basicSeries = GetBasicSeries(23, 1);

            var secondComp = basicSeries.Competitors.Skip(1).First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == secondComp).Code = "DSQ";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == secondComp).Place = 2;

            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.Equal(0m,
                results.Results[secondComp].CalculatedScores.Last().Value.ScoreValue);
        }

        [Fact]
        public void CalculateResults_SixthDsq_DoesNotChangethirdPlusPenalty()
        {
            var basicSeries = GetBasicSeries(23, 1);
            var thirdComp = basicSeries.Competitors.Skip(2).First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == thirdComp).Code = "ZFP";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == thirdComp).Place = 3;

            var sixthComp = basicSeries.Competitors.Skip(5).First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == sixthComp).Code = "DSQ";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == sixthComp).Place = 6;

            var results = _defaultCalculator.CalculateResults(basicSeries);

            // third place is 21, penalty is 5
            Assert.Equal(21m - 5m,
                results.Results[thirdComp].CalculatedScores.Last().Value.ScoreValue);
        }

        [Fact]
        public void CalculateResults_TiedForThird_BothGetThreeAndHalf()
        {
            var basicSeries = GetBasicSeries(10, 1);
            var thirdComp = basicSeries.Competitors.Skip(2).First();
            var fourthComp = basicSeries.Competitors.Skip(3).First();
            basicSeries.Races.Last().Scores.First(s => s.Competitor == fourthComp).Code = "TIE";
            basicSeries.Races.Last().Scores.First(s => s.Competitor == fourthComp).Place = 3;
            
            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.Equal(7.5m,
                results.Results[thirdComp].CalculatedScores.Last().Value.ScoreValue);
            Assert.Equal(7.5m,
                results.Results[fourthComp].CalculatedScores.Last().Value.ScoreValue);
        }

        [Fact]
        public void CalculateResults_CompWithHalfRaces_NotRanked()
        {
            var basicSeries = GetBasicSeries(4, 4);
            var firstComp = basicSeries.Competitors.First();
            basicSeries.Races.First().Scores.First(s => s.Competitor == firstComp).Code = "DNC";
            basicSeries.Races.Skip(1).First().Scores.First(s => s.Competitor == firstComp).Code = "DNC";
            
            var results = _defaultCalculator.CalculateResults(basicSeries);
            Assert.Null(results.Results[firstComp].TotalScore);
            Assert.True(results.Results[firstComp].Rank == null || results.Results[firstComp].Rank > 3);
        }

        // https://www.rya.org.uk/SiteCollectionDocuments/Racing/RacingInformation/RaceOfficials/Resource%20Centre/Best%20Practice%20Guidelines%20Policies/Scoring.pdf

        // Example: Scoring: Low Point – one score excluded
        // Race No: 1 2 3 4 5 6 TOTAL REORDERED COUNTING SCORES SCORES NOT USED
        // Boat A 3 4 1 6 2 7 16 1 2 3 4 6 7
        // Boat B 4 3 2 1 6 6 16 1 2 3 4 6 6
        // Boat C 1 2 7 3 3 14 16 1 2 3 3 7 14
        // Rule A8.1 is sometimes known as ‘most firsts, etc.’ It breaks the tie between C and the two other boats in C’s favour.It does not
        // break the tie between A and B. Rule A8.2 must now be applied to break that tie (in favour of B, for her better last race score). 
        [Fact]
        public void CalculateResults_SeriesTieBreaker_IgnoresDiscards()
        {
            var basicSeries = GetBasicSeries(15, 6);
            var firstComp = basicSeries.Competitors.First();
            var secondComp = basicSeries.Competitors.Skip(1).First();
            var thirdComp = basicSeries.Competitors.Skip(2).First();

            var firstRace = basicSeries.Races.First();
            firstRace.Scores.First(s => s.Competitor == firstComp).Place = 3;
            firstRace.Scores.First(s => s.Competitor == secondComp).Place = 4;
            firstRace.Scores.First(s => s.Competitor == thirdComp).Place = 1;
            firstRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(3).First()).Place = 2;

            var secondRace = basicSeries.Races.Skip(1).First();
            secondRace.Scores.First(s => s.Competitor == firstComp).Place = 4;
            secondRace.Scores.First(s => s.Competitor == secondComp).Place = 3;
            secondRace.Scores.First(s => s.Competitor == thirdComp).Place = 2;
            secondRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(3).First()).Place = 1;

            var thirdRace = basicSeries.Races.Skip(2).First();
            thirdRace.Scores.First(s => s.Competitor == thirdComp).Place = 7;
            thirdRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(6).First()).Place = 3;

            var fourthRace = basicSeries.Races.Skip(3).First();
            fourthRace.Scores.First(s => s.Competitor == firstComp).Place = 6;
            fourthRace.Scores.First(s => s.Competitor == secondComp).Place = 1;
            fourthRace.Scores.First(s => s.Competitor == thirdComp).Place = 3;
            fourthRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(5).First()).Place = 2;

            var fifthRace = basicSeries.Races.Skip(4).First();
            fifthRace.Scores.First(s => s.Competitor == firstComp).Place = 2;
            fifthRace.Scores.First(s => s.Competitor == secondComp).Place = 6;
            fifthRace.Scores.First(s => s.Competitor == thirdComp).Place = 3;
            fifthRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(5).First()).Place = 1;

            var sixthRace = basicSeries.Races.Skip(5).First();
            sixthRace.Scores.First(s => s.Competitor == firstComp).Place = 7;
            sixthRace.Scores.First(s => s.Competitor == secondComp).Place = 6;
            sixthRace.Scores.First(s => s.Competitor == thirdComp).Place = 14;
            sixthRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(5).First()).Place = 3;
            sixthRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(6).First()).Place = 2;
            sixthRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(13).First()).Place = 1;


            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.True(results.Results[thirdComp].Rank < results.Results[secondComp].Rank, "Comp 2 beats Comp 1");
            Assert.True(results.Results[thirdComp].Rank < results.Results[firstComp].Rank, "Comp 2 beats Comp 0");
        }

        // Example: Scoring: High Point Percent – one score excluded.
        // Race No:  1  2  3  4  TOTAL   1  2  3  4  TotalwDDisc
        // Boat A    3  4  5  10 12      13 12 11 6   36/48
        // Boat B    11 3  4  5  12      5  13 12 11  36/48
        // Boat C    5  15 3  4  12      11 1  13 12  36/48
        // Boat D    4  5  6  3  12      12 11 10 13  36/48
        // A8.1 does not break any tie, as they each have scores of 13, 12, 11 that count.
        // A8.2 applies, and the tie is broken in the order of D, C, B, A, the order of their last race scores.Note that A’s race 4 result was
        // her discard, but it is still used to break the tie.

        // Normally, the last race will resolve most ties.The next-to-last race (and so on) will need to be used only if two boats have the
        // same score in the last race, which might result from a ZFP, from a tie on the water or on handicap, or from both receiving nonfinishing points resulting from DNC, DNS, OCS, BFD, DNF, RAF, DSQ, DNE or DGM.
        [Fact]
        public void CalculateResults_SeriesTieBreaker_UsesLastRace()
        {
            var basicSeries = GetBasicSeries(15, 4);
            var firstComp = basicSeries.Competitors.First();
            var secondComp = basicSeries.Competitors.Skip(1).First();
            var thirdComp = basicSeries.Competitors.Skip(2).First();
            var fourthComp = basicSeries.Competitors.Skip(3).First();

            var firstRace = basicSeries.Races.First();
            firstRace.Scores.First(s => s.Competitor == firstComp).Place = 3;
            firstRace.Scores.First(s => s.Competitor == secondComp).Place = 11;
            firstRace.Scores.First(s => s.Competitor == thirdComp).Place = 5;
            firstRace.Scores.First(s => s.Competitor == fourthComp).Place = 4;
            firstRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(10).First()).Place = 2;
            firstRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(4).First()).Place = 1;

            var secondRace = basicSeries.Races.Skip(1).First();
            secondRace.Scores.First(s => s.Competitor == firstComp).Place = 4;
            secondRace.Scores.First(s => s.Competitor == secondComp).Place = 3;
            secondRace.Scores.First(s => s.Competitor == thirdComp).Place = 15;
            secondRace.Scores.First(s => s.Competitor == fourthComp).Place = 5;
            secondRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(14).First()).Place = 2;
            secondRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(4).First()).Place = 1;

            var thirdRace = basicSeries.Races.Skip(2).First();
            thirdRace.Scores.First(s => s.Competitor == firstComp).Place = 5;
            thirdRace.Scores.First(s => s.Competitor == secondComp).Place = 4;
            thirdRace.Scores.First(s => s.Competitor == thirdComp).Place = 3;
            thirdRace.Scores.First(s => s.Competitor == fourthComp).Place = 6;
            thirdRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(4).First()).Place = 2;
            thirdRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(5).First()).Place = 1;

            var fourthRace = basicSeries.Races.Skip(3).First();
            fourthRace.Scores.First(s => s.Competitor == firstComp).Place = 10;
            fourthRace.Scores.First(s => s.Competitor == secondComp).Place = 5;
            fourthRace.Scores.First(s => s.Competitor == thirdComp).Place = 4;
            fourthRace.Scores.First(s => s.Competitor == fourthComp).Place = 3;
            fourthRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(4).First()).Place = 2;
            fourthRace.Scores.First(s => s.Competitor == basicSeries.Competitors.Skip(9).First()).Place = 1;


            var results = _defaultCalculator.CalculateResults(basicSeries);

            Assert.True(results.Results[fourthComp].Rank < results.Results[thirdComp].Rank, "Comp 3 beats Comp 2");
            Assert.True(results.Results[thirdComp].Rank < results.Results[secondComp].Rank, "Comp 2 beats comp 1");
            Assert.True(results.Results[secondComp].Rank < results.Results[firstComp].Rank, "Comp 1 beats comp 0");
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
                        Order = i + 1,
                        Date = DateTime.UtcNow

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
