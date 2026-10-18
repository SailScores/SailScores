using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring
{
    // Builds the scoring columns stored with flattened results. Which columns a calculator
    // produces comes from ScoringCalculatorRegistry.GetColumns.
    public static class ScoringColumns
    {
        public const string PreDiscardTotalKey = "preDiscardTotal";
        public const string ParticipationKey = "participation";
        public const string PenaltiesKey = "penalties";

        public static readonly ScoringColumnDescriptor PreDiscardTotal = new(
            PreDiscardTotalKey,
            "Pre-Discard Total",
            "Total of all race scores, including discards. A results template can also show this column.",
            ScoringColumnFormat.Decimal,
            ColumnVisibility.Hidden,
            includeForPercentSystems: false,
            valueFromScore: s => Value(s.TotalScoreIncludingDiscards
                // Results saved before this was recorded.
                ?? s.Scores?.Sum(c => c.ScoreValue ?? 0m)));

        public static readonly ScoringColumnDescriptor Participation = new(
            ParticipationKey,
            "Participation",
            "Percent of races sailed.",
            ScoringColumnFormat.Percent,
            ColumnVisibility.Hidden,
            valueFromScore: s => Value(s.ParticipationPercent));

        public static readonly ScoringColumnDescriptor Penalties = new(
            PenaltiesKey,
            "Penalties",
            "Penalty scores added for low participation. Only shown when a competitor has one.",
            ScoringColumnFormat.Decimal,
            ColumnVisibility.Always,
            onlyWhenAnyValue: true);

        public const string NetPointsKey = "netPoints";
        public const string NetRacesKey = "netRaces";

        // The average's numerator and denominator (PointsEarned / PointsPossible).
        public static readonly ScoringColumnDescriptor NetPoints = new(
            NetPointsKey,
            "Net Points",
            "Total of the race scores counted in the score (after discards).",
            ScoringColumnFormat.Decimal,
            ColumnVisibility.Always,
            valueFromScore: s => Value(s.PointsEarned));

        public static readonly ScoringColumnDescriptor NetRaces = new(
            NetRacesKey,
            "Net Series Races",
            "Number of races the score is averaged over (after discards).",
            ScoringColumnFormat.Decimal,
            ColumnVisibility.Always,
            valueFromScore: s => Value(s.PointsPossible));

        public const string AverageFinishKey = "averageFinish";

        public static readonly ScoringColumnDescriptor AverageFinish = new(
            AverageFinishKey,
            "Average",
            "Average finish over the races that count; used to break ties.",
            ScoringColumnFormat.Decimal,
            ColumnVisibility.Always,
            valueFromScore: s => Value(s.Average));

        public const string AverageWithPenaltiesKey = "averageWithPenalties";

        // Wawasee 2026: the total with penalties over the races that count.
        public static readonly ScoringColumnDescriptor AverageWithPenalties = new(
            AverageWithPenaltiesKey,
            "Average (with Penalties)",
            "Points, including penalties, divided by the number of races that count.",
            ScoringColumnFormat.Decimal,
            ColumnVisibility.Hidden,
            valueFromScore: s => s.PointsPossible > 0
                ? Value(s.PointsEarned / s.PointsPossible)
                : null);

        // Columns every calculator produces.
        public static readonly IReadOnlyList<ScoringColumnDescriptor> Standard = new[]
        {
            PreDiscardTotal,
            Participation
        };

        // Fills values for columns built from score properties, and returns the columns
        // with their visibility for this scoring system. scores must be a materialized list.
        public static IList<FlatResultColumn> Resolve(
            IEnumerable<ScoringColumnDescriptor> descriptors,
            ScoringSystemSettings effectiveSettings,
            bool isPercentSystem,
            IList<FlatSeriesScore> scores)
        {
            var columns = new List<FlatResultColumn>();
            foreach (var descriptor in descriptors)
            {
                if (isPercentSystem && !descriptor.IncludeForPercentSystems)
                {
                    continue;
                }
                if (descriptor.ValueFromScore != null)
                {
                    foreach (var score in scores)
                    {
                        var value = descriptor.ValueFromScore(score);
                        if (value != null)
                        {
                            score.ColumnValues ??= new Dictionary<string, ScoringColumnValue>();
                            score.ColumnValues[descriptor.Key] = value;
                        }
                    }
                }
                if (descriptor.OnlyWhenAnyValue
                    && !scores.Any(s => s.ColumnValues?.ContainsKey(descriptor.Key) == true))
                {
                    continue;
                }
                columns.Add(new FlatResultColumn
                {
                    Key = descriptor.Key,
                    Header = descriptor.Header,
                    Format = descriptor.Format,
                    Visibility = effectiveSettings?.GetColumnVisibility(descriptor.Key)
                        ?? descriptor.DefaultVisibility
                });
            }
            return columns;
        }

        // Results saved before scoring columns existed get the standard columns with
        // default visibility.
        public static void AddLegacyColumns(FlatResults results)
        {
            if (results == null || results.Columns != null)
            {
                return;
            }
            results.Columns = Resolve(
                Standard,
                null,
                results.IsPercentSystem,
                results.CalculatedScores?.ToList() ?? new List<FlatSeriesScore>());
        }

        private static ScoringColumnValue Value(decimal? value)
        {
            return value == null ? null : new ScoringColumnValue { Value = value };
        }
    }
}
