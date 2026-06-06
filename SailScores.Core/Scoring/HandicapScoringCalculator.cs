using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{
    // Decorator that applies time-correction (Phase 0) before delegating to the inner
    // (place-based) series calculator. Works with any IScoringCalculator as its inner,
    // so clubs can combine handicap time correction with any series scoring system.
    //
    // IRC/ORC support: use HandicapSystemType.TimeOnTime with a TCC stored as Value and
    // set EffectiveTo to the certificate expiry date.
    public class HandicapScoringCalculator : IScoringCalculator
    {
        // Assigned when a competitor has no handicap and no explicit finish code.
        public const string NhcCode = "NHC";

        private readonly IScoringCalculator _innerCalculator;
        private readonly HandicapSystem _handicapSystem;
        private readonly IReadOnlyDictionary<(Guid competitorId, DateTime raceDate), decimal> _handicapLookup;

        public HandicapScoringCalculator(
            IScoringCalculator innerCalculator,
            HandicapSystem handicapSystem,
            IReadOnlyDictionary<(Guid competitorId, DateTime raceDate), decimal> handicapLookup)
        {
            _innerCalculator = innerCalculator;
            _handicapSystem = handicapSystem;
            _handicapLookup = handicapLookup;
        }

        public SeriesResults CalculateResults(Series series)
        {
            ApplyTimeCorrection(series);
            return _innerCalculator.CalculateResults(series);
        }

        // Phase 0: for each raced race, compute corrected times, then re-rank by corrected time.
        // Modifies score.Place, score.CorrectedTime, score.HandicapValue, and score.Code in place.
        private void ApplyTimeCorrection(Series series)
        {
            var racedStates = new[] { RaceState.Raced, RaceState.Preliminary };
            foreach (var race in series.Races ?? Enumerable.Empty<Race>())
            {
                if (race.Scores == null) continue;
                var state = race.State ?? RaceState.Raced;
                if (!racedStates.Contains(state)) continue;

                var raceDate = race.Date ?? DateTime.MinValue;

                foreach (var score in race.Scores)
                {
                    // Skip scores that already carry a finish code (DNS, DNC, etc.)
                    if (!string.IsNullOrEmpty(score.Code)) continue;

                    // Resolve handicap: manual override on the score takes priority
                    decimal? handicap = score.HandicapValue;
                    if (!handicap.HasValue && raceDate != DateTime.MinValue)
                    {
                        // Use the bool return of TryGetValue — do NOT compare the value to default(decimal)
                        // because 0 is a valid PHRF scratch-boat rating.
                        if (_handicapLookup.TryGetValue((score.CompetitorId, raceDate.Date), out var looked))
                            handicap = looked;
                    }

                    if (!handicap.HasValue)
                    {
                        score.Code = NhcCode;
                        continue;
                    }

                    if (!score.ElapsedTime.HasValue) continue;

                    score.HandicapValue = handicap;
                    score.CorrectedTime = ComputeCorrectedTime(
                        score.ElapsedTime.Value, handicap.Value, race.CourseDistance);
                }

                // Re-rank by corrected time; boats without a corrected time keep Place = null
                var ranked = race.Scores
                    .Where(s => s.CorrectedTime.HasValue && string.IsNullOrEmpty(s.Code))
                    .OrderBy(s => s.CorrectedTime!.Value)
                    .ToList();

                for (int i = 0; i < ranked.Count; i++)
                {
                    ranked[i].Place = i + 1;
                }

                // Nullify place for scores that have a code or no corrected time
                foreach (var score in race.Scores.Where(s => !s.CorrectedTime.HasValue || !string.IsNullOrEmpty(s.Code)))
                {
                    if (string.IsNullOrEmpty(score.Code))
                        score.Place = null;
                }
            }
        }

        public static TimeSpan ComputeCorrectedTime(
            TimeSpan elapsed,
            decimal handicapValue,
            HandicapSystemType systemType,
            decimal? courseDistance)
        {
            return systemType switch
            {
                HandicapSystemType.PhrfToD => PhrfToD(elapsed, handicapValue, courseDistance),
                HandicapSystemType.PhrfToT => PhrfToT(elapsed, handicapValue),
                HandicapSystemType.Portsmouth => Portsmouth(elapsed, handicapValue),
                HandicapSystemType.TimeOnTime => TimeOnTime(elapsed, handicapValue),
                HandicapSystemType.PortsmouthDpy => PortsmouthDpy(elapsed, handicapValue),
                _ => throw new InvalidOperationException(
                    $"Unsupported handicap system type: {systemType}")
            };
        }

        private TimeSpan ComputeCorrectedTime(TimeSpan elapsed, decimal handicapValue, decimal? courseDistance)
        {
            return ComputeCorrectedTime(elapsed, handicapValue, _handicapSystem.SystemType, courseDistance);
        }

        // corrected = elapsed_sec - (rating × distance_nm)
        private static TimeSpan PhrfToD(TimeSpan elapsed, decimal rating, decimal? distanceNm)
        {
            if (!distanceNm.HasValue || distanceNm.Value <= 0m)
                throw new InvalidOperationException(
                    "Course distance must be set on the race for PHRF Time-on-Distance scoring.");

            var correctedSeconds = (double)(elapsed.TotalSeconds - (double)(rating * distanceNm.Value));
            return TimeSpan.FromSeconds(correctedSeconds);
        }

        // corrected = elapsed_sec × 600 / (600 + rating)
        private static TimeSpan PhrfToT(TimeSpan elapsed, decimal rating)
        {
            var factor = 600.0 / (600.0 + (double)rating);
            return TimeSpan.FromSeconds(elapsed.TotalSeconds * factor);
        }

        // corrected = elapsed_sec / PY × 1000
        private static TimeSpan Portsmouth(TimeSpan elapsed, decimal py)
        {
            if (py == 0m)
                throw new InvalidOperationException("Portsmouth Yardstick rating must not be zero.");

            var correctedSeconds = elapsed.TotalSeconds / (double)py * 1000.0;
            return TimeSpan.FromSeconds(correctedSeconds);
        }

        // corrected = elapsed_sec / PY × 100
        private static TimeSpan PortsmouthDpy(TimeSpan elapsed, decimal py)
        {
            if (py == 0m)
                throw new InvalidOperationException("Portsmouth Yardstick rating must not be zero.");

            var correctedSeconds = elapsed.TotalSeconds / (double)py * 100.0;
            return TimeSpan.FromSeconds(correctedSeconds);
        }

        // Generic time-on-time: corrected = elapsed_sec × coefficient
        // Covers IRC/ORC when the TCC is stored as Value.
        private static TimeSpan TimeOnTime(TimeSpan elapsed, decimal coefficient)
        {
            return TimeSpan.FromSeconds(elapsed.TotalSeconds * (double)coefficient);
        }
    }
}
