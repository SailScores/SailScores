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
            var inner = new AppendixACalculator(MakeScoringSystem());
            return new HandicapScoringCalculator(inner, MakeHandicapSystem(type),
                lookup ?? new Dictionary<(Guid, DateTime), decimal>());
        }

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
            calculator.CalculateResults(series);

            Assert.Equal(2, scoreA.Place); // 3150 s → 2nd
            Assert.Equal(1, scoreB.Place); // 2900 s → 1st
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
            calculator.CalculateResults(series);

            // corrected = 3600 - 0×5 = 3600 s (unchanged for scratch)
            Assert.Equal(TimeSpan.FromSeconds(3600), score.CorrectedTime);
            Assert.Equal(1, score.Place);
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
            calculator.CalculateResults(series);

            var expected = 3600.0 * 600.0 / 690.0;
            Assert.NotNull(score.CorrectedTime);
            Assert.Equal(expected, score.CorrectedTime!.Value.TotalSeconds, precision: 1);
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
            calculator.CalculateResults(series);

            var expected = 3600.0 / 1050.0 * 1000.0;
            Assert.NotNull(score.CorrectedTime);
            Assert.Equal(expected, score.CorrectedTime!.Value.TotalSeconds, precision: 1);
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
            portsmouthCalculator.CalculateResults(seriesPortsmouth);
            var pyCorrected = score.CorrectedTime;

            var raceDpy = MakeRace(raceDate);
            var scoreDpy = MakeTimedScore(comp, raceDpy, TimeSpan.FromSeconds(3600));
            raceDpy.Scores = new List<Score> { scoreDpy };
            var seriesDpy = new Series { Races = new List<Race> { raceDpy }, ScoringSystem = MakeScoringSystem() };
            scoreDpy.Competitor = comp;

            var dpyCalculator = MakeCalculator(HandicapSystemType.PortsmouthDpy, lookup);
            dpyCalculator.CalculateResults(seriesDpy);

            Assert.NotNull(pyCorrected);
            Assert.NotNull(scoreDpy.CorrectedTime);
            Assert.Equal(pyCorrected!.Value.TotalSeconds / 10.0, scoreDpy.CorrectedTime!.Value.TotalSeconds, precision: 1);
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
            portsmouthCalculator.CalculateResults(seriesPortsmouth);

            var raceDpy = MakeRace(raceDate);
            var scoreDpyA = MakeTimedScore(compA, raceDpy, TimeSpan.FromSeconds(3500));
            var scoreDpyB = MakeTimedScore(compB, raceDpy, TimeSpan.FromSeconds(3600));
            raceDpy.Scores = new List<Score> { scoreDpyA, scoreDpyB };

            var seriesDpy = new Series { Races = new List<Race> { raceDpy }, ScoringSystem = MakeScoringSystem() };
            scoreDpyA.Competitor = compA;
            scoreDpyB.Competitor = compB;

            var dpyCalculator = MakeCalculator(HandicapSystemType.PortsmouthDpy, lookup);
            dpyCalculator.CalculateResults(seriesDpy);

            Assert.Equal(scorePortsmouthA.Place, scoreDpyA.Place);
            Assert.Equal(scorePortsmouthB.Place, scoreDpyB.Place);
            Assert.True(scoreDpyA.Place < scoreDpyB.Place);
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
            calculator.CalculateResults(series);

            Assert.Equal(HandicapScoringCalculator.NhcCode, score.Code);
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
            calculator.CalculateResults(series);

            // corrected = 3600 - 60×5 = 3300 s (using override 60, not lookup 90)
            Assert.NotNull(score.CorrectedTime);
            Assert.Equal(3300.0, score.CorrectedTime!.Value.TotalSeconds, precision: 1);
            Assert.Equal(60m, score.HandicapValue);
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
            calculator.CalculateResults(series);

            Assert.Equal("DNS", score.Code);
            Assert.Null(score.CorrectedTime);
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
    }
}
