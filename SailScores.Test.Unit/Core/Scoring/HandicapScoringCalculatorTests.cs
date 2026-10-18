using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring
{
    public class HandicapScoringCalculatorTests
    {
        // --- Helpers ---

        private static HandicapSystem MakeHandicapSystem(HandicapSystemType type)
            => new HandicapSystem { Id = Guid.NewGuid(), Name = type.ToString(), SystemType = type };

        private static ScoringSystem MakeScoringSystem()
        {
            var id = Guid.NewGuid();
            return new ScoringSystem
            {
                Id = id,
                Name = "Appendix A Low Point",
                DiscardPattern = "0",
                ScoreCodes = new List<ScoreCode>
                {
                    new ScoreCode { Name = "DNC", Formula = "SER+", FormulaValue = 1,
                        Discardable = true, CameToStart = false, Started = false,
                        Finished = false, AdjustOtherScores = false, ScoringSystemId = id },
                    new ScoreCode { Name = "DNS", Formula = "CTS+", FormulaValue = 1,
                        Discardable = true, CameToStart = true, Started = false,
                        Finished = false, AdjustOtherScores = false, ScoringSystemId = id },
                    new ScoreCode { Name = "DNF", Formula = "FIN+", FormulaValue = 1,
                        Discardable = true, CameToStart = true, Started = true,
                        Finished = false, AdjustOtherScores = false, ScoringSystemId = id },
                },
                InheritedScoreCodes = new List<ScoreCode>()
            };
        }

        private static Competitor MakeCompetitor() =>
            new Competitor { Id = Guid.NewGuid(), Name = "Test Sailor", SailNumber = "42" };

        private static Race MakeRace(DateTime date, decimal? courseDistance = null)
            => new Race
            {
                Id = Guid.NewGuid(),
                Date = date,
                Order = 1,
                State = RaceState.Raced,
                CourseDistance = courseDistance,
                Scores = new List<Score>()
            };

        private static Score MakeTimedScore(Competitor comp, Race race, TimeSpan elapsed,
            decimal? handicapOverride = null)
            => new Score
            {
                Id = Guid.NewGuid(),
                CompetitorId = comp.Id,
                Competitor = comp,
                Race = race,
                RaceId = race.Id,
                ElapsedTime = elapsed,
                HandicapValue = handicapOverride
            };

        private static Score MakeCodeScore(Competitor comp, Race race, string code)
            => new Score
            {
                Id = Guid.NewGuid(),
                CompetitorId = comp.Id,
                Competitor = comp,
                Race = race,
                RaceId = race.Id,
                Code = code
            };

        private static HandicapScoringCalculator MakeCalculator(
            HandicapSystemType type,
            IReadOnlyDictionary<(Guid, DateTime), decimal> lookup = null)
        {
            var inner = ScoringCalculatorRegistry.CreateCalculator(ScoringCalculatorType.AppendixA, MakeScoringSystem());
            return new HandicapScoringCalculator(inner, MakeHandicapSystem(type),
                lookup ?? new Dictionary<(Guid, DateTime), decimal>());
        }

        // The score as the run scored it (its corrected time, place and code). The series'
        // own scores are left as they were.
        private static Score Scored(SeriesResults results, Score input) =>
            results.Results.Single(r => r.Key.Id == input.CompetitorId).Value
                .CalculatedScores[input.Race].RawScore;

        // --- PHRF Time-on-Distance formula tests ---

        [Fact]
        public void CalculateResults_PhrfToD_RanksByCorrecterTime()
        {
            var raceDate = new DateTime(2025, 6, 1);
            var compA = MakeCompetitor();
            var compB = MakeCompetitor();
            var race = MakeRace(raceDate, courseDistance: 5m); // 5 nautical miles

            // compA: rating 90 (slower), elapsed 3600 s → corrected = 3600 - 90×5 = 3150 s
            // compB: rating 120 (faster), elapsed 3500 s → corrected = 3500 - 120×5 = 2900 s
            // compB should finish 1st on corrected time
            var scoreA = MakeTimedScore(compA, race, TimeSpan.FromSeconds(3600));
            var scoreB = MakeTimedScore(compB, race, TimeSpan.FromSeconds(3500));
            race.Scores = new List<Score> { scoreA, scoreB };

            var lookup = new Dictionary<(Guid, DateTime), decimal>
            {
                { (compA.Id, raceDate.Date), 90m },
                { (compB.Id, raceDate.Date), 120m }
            };

            var series = new Series
            {
                Races = new List<Race> { race },
                ScoringSystem = MakeScoringSystem()
            };
            series.Races[0].Scores[0].Competitor = compA;
            series.Races[0].Scores[1].Competitor = compB;

            var calculator = MakeCalculator(HandicapSystemType.PhrfToD, lookup);
            var results = calculator.CalculateResults(series);

            Assert.Equal(2, Scored(results, scoreA).Place); // 3150 s → 2nd
            Assert.Equal(1, Scored(results, scoreB).Place); // 2900 s → 1st
        }

        [Fact]
        public void CalculateResults_PhrfToD_ScratchBoatRatingZero_CorrectsToElapsedTime()
        {
            var raceDate = new DateTime(2025, 6, 1);
            var comp = MakeCompetitor();
            var race = MakeRace(raceDate, courseDistance: 5m);
            var score = MakeTimedScore(comp, race, TimeSpan.FromSeconds(3600));
            race.Scores = new List<Score> { score };

            var lookup = new Dictionary<(Guid, DateTime), decimal>
            {
                { (comp.Id, raceDate.Date), 0m } // scratch boat
            };

            var series = new Series { Races = new List<Race> { race }, ScoringSystem = MakeScoringSystem() };
            score.Competitor = comp;

            var calculator = MakeCalculator(HandicapSystemType.PhrfToD, lookup);
            var scored = Scored(calculator.CalculateResults(series), score);

            // corrected = 3600 - 0×5 = 3600 s (unchanged for scratch)
            Assert.Equal(TimeSpan.FromSeconds(3600), scored.CorrectedTime);
            Assert.Equal(1, scored.Place);
        }

        // --- PHRF Time-on-Time formula tests ---

        [Fact]
        public void CalculateResults_PhrfToT_AppliesCorrectFormula()
        {
            // corrected = elapsed × 600 / (600 + rating)
            // comp: rating 90, elapsed 3600 → corrected = 3600 × 600/690 ≈ 3130.4 s
            var raceDate = new DateTime(2025, 6, 1);
            var comp = MakeCompetitor();
            var race = MakeRace(raceDate); // no distance needed for ToT
            var score = MakeTimedScore(comp, race, TimeSpan.FromSeconds(3600));
            race.Scores = new List<Score> { score };

            var lookup = new Dictionary<(Guid, DateTime), decimal>
            {
                { (comp.Id, raceDate.Date), 90m }
            };

            var series = new Series { Races = new List<Race> { race }, ScoringSystem = MakeScoringSystem() };
            score.Competitor = comp;

            var calculator = MakeCalculator(HandicapSystemType.PhrfToT, lookup);
            var scored = Scored(calculator.CalculateResults(series), score);

            var expected = 3600.0 * 600.0 / 690.0;
            Assert.NotNull(scored.CorrectedTime);
            Assert.Equal(expected, scored.CorrectedTime!.Value.TotalSeconds, precision: 1);
        }

        // --- Portsmouth Yardstick formula tests ---

        [Fact]
        public void CalculateResults_Portsmouth_AppliesCorrectFormula()
        {
            // corrected = elapsed_sec / PY × 1000
            // comp: PY 1050, elapsed 3600 → corrected = 3600/1050×1000 ≈ 3428.6 s
            var raceDate = new DateTime(2025, 6, 1);
            var comp = MakeCompetitor();
            var race = MakeRace(raceDate);
            var score = MakeTimedScore(comp, race, TimeSpan.FromSeconds(3600));
            race.Scores = new List<Score> { score };

            var lookup = new Dictionary<(Guid, DateTime), decimal>
            {
                { (comp.Id, raceDate.Date), 1050m }
            };

            var series = new Series { Races = new List<Race> { race }, ScoringSystem = MakeScoringSystem() };
            score.Competitor = comp;

            var calculator = MakeCalculator(HandicapSystemType.Portsmouth, lookup);
            var scored = Scored(calculator.CalculateResults(series), score);

            var expected = 3600.0 / 1050.0 * 1000.0;
            Assert.NotNull(scored.CorrectedTime);
            Assert.Equal(expected, scored.CorrectedTime!.Value.TotalSeconds, precision: 1);
        }

        [Fact]
        public void CalculateResults_PortsmouthDpy_IsOneTenthOfPortsmouth()
        {
            var raceDate = new DateTime(2025, 6, 1);
            var comp = MakeCompetitor();
            var race = MakeRace(raceDate);
            var score = MakeTimedScore(comp, race, TimeSpan.FromSeconds(3600));
            race.Scores = new List<Score> { score };

            var lookup = new Dictionary<(Guid, DateTime), decimal>
            {
                { (comp.Id, raceDate.Date), 1050m }
            };

            var seriesPortsmouth = new Series { Races = new List<Race> { race }, ScoringSystem = MakeScoringSystem() };
            score.Competitor = comp;

            var portsmouthCalculator = MakeCalculator(HandicapSystemType.Portsmouth, lookup);
            var pyCorrected = Scored(portsmouthCalculator.CalculateResults(seriesPortsmouth), score).CorrectedTime;

            var raceDpy = MakeRace(raceDate);
            var scoreDpy = MakeTimedScore(comp, raceDpy, TimeSpan.FromSeconds(3600));
            raceDpy.Scores = new List<Score> { scoreDpy };
            var seriesDpy = new Series { Races = new List<Race> { raceDpy }, ScoringSystem = MakeScoringSystem() };
            scoreDpy.Competitor = comp;

            var dpyCalculator = MakeCalculator(HandicapSystemType.PortsmouthDpy, lookup);
            var dpyCorrected = Scored(dpyCalculator.CalculateResults(seriesDpy), scoreDpy).CorrectedTime;

            Assert.NotNull(pyCorrected);
            Assert.NotNull(dpyCorrected);
            Assert.Equal(pyCorrected!.Value.TotalSeconds / 10.0, dpyCorrected!.Value.TotalSeconds, precision: 1);
        }

        [Fact]
        public void CalculateResults_PortsmouthDpy_PreservesRankingFromPortsmouth()
        {
            var raceDate = new DateTime(2025, 6, 1);
            var compA = MakeCompetitor();
            var compB = MakeCompetitor();

            var lookup = new Dictionary<(Guid, DateTime), decimal>
            {
                { (compA.Id, raceDate.Date), 1000m },
                { (compB.Id, raceDate.Date), 1000m }
            };

            var racePortsmouth = MakeRace(raceDate);
            var scorePortsmouthA = MakeTimedScore(compA, racePortsmouth, TimeSpan.FromSeconds(3500));
            var scorePortsmouthB = MakeTimedScore(compB, racePortsmouth, TimeSpan.FromSeconds(3600));
            racePortsmouth.Scores = new List<Score> { scorePortsmouthA, scorePortsmouthB };

            var seriesPortsmouth = new Series { Races = new List<Race> { racePortsmouth }, ScoringSystem = MakeScoringSystem() };
            scorePortsmouthA.Competitor = compA;
            scorePortsmouthB.Competitor = compB;

            var portsmouthCalculator = MakeCalculator(HandicapSystemType.Portsmouth, lookup);
            var portsmouthResults = portsmouthCalculator.CalculateResults(seriesPortsmouth);

            var raceDpy = MakeRace(raceDate);
            var scoreDpyA = MakeTimedScore(compA, raceDpy, TimeSpan.FromSeconds(3500));
            var scoreDpyB = MakeTimedScore(compB, raceDpy, TimeSpan.FromSeconds(3600));
            raceDpy.Scores = new List<Score> { scoreDpyA, scoreDpyB };

            var seriesDpy = new Series { Races = new List<Race> { raceDpy }, ScoringSystem = MakeScoringSystem() };
            scoreDpyA.Competitor = compA;
            scoreDpyB.Competitor = compB;

            var dpyCalculator = MakeCalculator(HandicapSystemType.PortsmouthDpy, lookup);
            var dpyResults = dpyCalculator.CalculateResults(seriesDpy);

            Assert.Equal(Scored(portsmouthResults, scorePortsmouthA).Place, Scored(dpyResults, scoreDpyA).Place);
            Assert.Equal(Scored(portsmouthResults, scorePortsmouthB).Place, Scored(dpyResults, scoreDpyB).Place);
            Assert.True(Scored(dpyResults, scoreDpyA).Place < Scored(dpyResults, scoreDpyB).Place);
        }

        // --- NHC code assignment ---

        [Fact]
        public void CalculateResults_CompetitorWithNoHandicap_AssignsNhcCode()
        {
            var raceDate = new DateTime(2025, 6, 1);
            var comp = MakeCompetitor();
            var race = MakeRace(raceDate, courseDistance: 5m);
            var score = MakeTimedScore(comp, race, TimeSpan.FromSeconds(3600));
            race.Scores = new List<Score> { score };

            // Empty lookup — no handicap for this competitor
            var series = new Series { Races = new List<Race> { race }, ScoringSystem = MakeScoringSystem() };
            score.Competitor = comp;

            var calculator = MakeCalculator(HandicapSystemType.PhrfToD);
            var results = calculator.CalculateResults(series);

            Assert.Equal(HandicapScoringCalculator.NhcCode, results.Results.Single().Value.CalculatedScores[race].Code);
            Assert.Null(score.Code);
        }

        // --- Manual handicap override on score ---

        [Fact]
        public void CalculateResults_ManualHandicapOverride_UsedInsteadOfLookup()
        {
            var raceDate = new DateTime(2025, 6, 1);
            var comp = MakeCompetitor();
            var race = MakeRace(raceDate, courseDistance: 5m);
            // Manual override of 60 on the score, but lookup has 90
            var score = MakeTimedScore(comp, race, TimeSpan.FromSeconds(3600), handicapOverride: 60m);
            race.Scores = new List<Score> { score };

            var lookup = new Dictionary<(Guid, DateTime), decimal>
            {
                { (comp.Id, raceDate.Date), 90m } // should be ignored
            };

            var series = new Series { Races = new List<Race> { race }, ScoringSystem = MakeScoringSystem() };
            score.Competitor = comp;

            var calculator = MakeCalculator(HandicapSystemType.PhrfToD, lookup);
            var scored = Scored(calculator.CalculateResults(series), score);

            // corrected = 3600 - 60×5 = 3300 s (using override 60, not lookup 90)
            Assert.NotNull(scored.CorrectedTime);
            Assert.Equal(3300.0, scored.CorrectedTime!.Value.TotalSeconds, precision: 1);
            Assert.Equal(60m, scored.HandicapValue);
        }

        // --- Boats with finish codes are not time-corrected ---

        [Fact]
        public void CalculateResults_BoatWithDnsCode_NotTimeCorrected()
        {
            var raceDate = new DateTime(2025, 6, 1);
            var comp = MakeCompetitor();
            var race = MakeRace(raceDate, courseDistance: 5m);
            var score = MakeCodeScore(comp, race, "DNS");
            race.Scores = new List<Score> { score };

            var lookup = new Dictionary<(Guid, DateTime), decimal>
            {
                { (comp.Id, raceDate.Date), 90m }
            };

            var series = new Series { Races = new List<Race> { race }, ScoringSystem = MakeScoringSystem() };
            score.Competitor = comp;

            var calculator = MakeCalculator(HandicapSystemType.PhrfToD, lookup);
            var scored = Scored(calculator.CalculateResults(series), score);

            Assert.Equal("DNS", scored.Code);
            Assert.Null(scored.CorrectedTime);
        }

        // --- PHRF ToD throws without course distance ---

        [Fact]
        public void CalculateResults_PhrfToD_NoCourseDistance_Throws()
        {
            var raceDate = new DateTime(2025, 6, 1);
            var comp = MakeCompetitor();
            var race = MakeRace(raceDate, courseDistance: null); // missing distance
            var score = MakeTimedScore(comp, race, TimeSpan.FromSeconds(3600));
            race.Scores = new List<Score> { score };

            var lookup = new Dictionary<(Guid, DateTime), decimal>
            {
                { (comp.Id, raceDate.Date), 90m }
            };

            var series = new Series { Races = new List<Race> { race }, ScoringSystem = MakeScoringSystem() };
            score.Competitor = comp;

            var calculator = MakeCalculator(HandicapSystemType.PhrfToD, lookup);
            Assert.Throws<InvalidOperationException>(() => calculator.CalculateResults(series));
        }

        // --- Formulas and edge cases ---

        [Fact]
        public void ComputeCorrectedTime_TimeOnTime_MultipliesByCoefficient()
        {
            var corrected = HandicapScoringCalculator.ComputeCorrectedTime(
                TimeSpan.FromSeconds(3600), 0.9m, HandicapSystemType.TimeOnTime, null);

            Assert.Equal(TimeSpan.FromSeconds(3240), corrected);
        }

        [Theory]
        [InlineData(HandicapSystemType.Portsmouth)]
        [InlineData(HandicapSystemType.PortsmouthDpy)]
        public void ComputeCorrectedTime_PortsmouthRatingZero_Throws(HandicapSystemType type)
        {
            Assert.Throws<InvalidOperationException>(() =>
                HandicapScoringCalculator.ComputeCorrectedTime(TimeSpan.FromSeconds(3600), 0m, type, null));
        }

        [Fact]
        public void ComputeCorrectedTime_UnknownSystemType_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                HandicapScoringCalculator.ComputeCorrectedTime(
                    TimeSpan.FromSeconds(3600), 1m, (HandicapSystemType)999, null));
        }

        [Fact]
        public void CalculateResults_RatedBoatWithoutElapsedTime_HasNoPlace()
        {
            var raceDate = new DateTime(2025, 6, 1);
            var timed = MakeCompetitor();
            var untimed = MakeCompetitor();
            var race = MakeRace(raceDate);
            var timedScore = MakeTimedScore(timed, race, TimeSpan.FromSeconds(3600), handicapOverride: 100m);
            var untimedScore = new Score
            {
                Id = Guid.NewGuid(), CompetitorId = untimed.Id, Competitor = untimed,
                Race = race, RaceId = race.Id, Place = 1, HandicapValue = 100m
            };
            race.Scores = new List<Score> { timedScore, untimedScore };
            var series = new Series { Races = new List<Race> { race }, ScoringSystem = MakeScoringSystem() };

            var results = MakeCalculator(HandicapSystemType.Portsmouth).CalculateResults(series);

            Assert.Equal(1, Scored(results, timedScore).Place);
            Assert.Null(Scored(results, untimedScore).Place);
        }

        [Fact]
        public void CalculateResults_TimedRace_LeavesSeriesScoresUnchanged()
        {
            var raceDate = new DateTime(2025, 6, 1);
            var timed = MakeCompetitor();
            var unrated = MakeCompetitor();
            var race = MakeRace(raceDate);
            var timedScore = MakeTimedScore(timed, race, TimeSpan.FromSeconds(3600));
            var unratedScore = MakeTimedScore(unrated, race, TimeSpan.FromSeconds(3000));
            unratedScore.Place = 1;
            race.Scores = new List<Score> { timedScore, unratedScore };
            var series = new Series { Races = new List<Race> { race }, ScoringSystem = MakeScoringSystem() };
            var lookup = new Dictionary<(Guid, DateTime), decimal> { { (timed.Id, raceDate.Date), 100m } };

            var results = MakeCalculator(HandicapSystemType.Portsmouth, lookup).CalculateResults(series);

            Assert.Equal(1, Scored(results, timedScore).Place);
            Assert.Equal(HandicapScoringCalculator.NhcCode, Scored(results, unratedScore).Code);
            Assert.Null(timedScore.Place);
            Assert.Null(timedScore.CorrectedTime);
            Assert.Null(timedScore.HandicapValue);
            Assert.Equal(1, unratedScore.Place);
            Assert.Null(unratedScore.Code);
        }

        [Fact]
        public void CalculateAbsentCompetitorScore_TimedRace_ScoresAsInnerCalculator()
        {
            var raceDate = new DateTime(2025, 6, 1);
            var comp = MakeCompetitor();
            var race = MakeRace(raceDate);
            race.Scores = new List<Score> { MakeTimedScore(comp, race, TimeSpan.FromSeconds(3600), handicapOverride: 100m) };
            var series = new Series
            {
                Races = new List<Race> { race },
                Competitors = new List<Competitor> { comp },
                ScoringSystem = MakeScoringSystem()
            };

            var absentScore = MakeCalculator(HandicapSystemType.Portsmouth).CalculateAbsentCompetitorScore(series);

            // DNC: one series competitor + 1.
            Assert.Equal(2m, absentScore);
        }
    }
}
