using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring.Systems;
using SailScores.Core.Services;

namespace SailScores.Core.Scoring
{
    // One entry per base scoring calculator: how to build it, which settings fields it
    // uses, which discard methods it supports, and which extra results columns it produces. Adding a new base system means adding
    // a calculator class and a registration here.
    public class ScoringCalculatorRegistration
    {
        public ScoringCalculatorType CalculatorType { get; init; }
        public Func<ScoringSystem, IScoringCalculator> Create { get; init; }
        public IReadOnlyList<ScoringFieldDescriptor> Fields { get; init; } = Array.Empty<ScoringFieldDescriptor>();
        public IReadOnlyList<DiscardMethod> AllowedDiscardMethods { get; init; } =
            ScoringCalculatorRegistry.PatternDiscardMethods;
        // Columns this calculator adds to ScoringColumns.Standard.
        public IReadOnlyList<ScoringColumnDescriptor> Columns { get; init; } = Array.Empty<ScoringColumnDescriptor>();
        // Optional partial view for settings that the generic field renderer can't handle.
        public string CustomSettingsPartial { get; init; }
        // Heading of the results' total column.
        public string TotalHeader { get; init; } = ScoringCalculatorRegistry.DefaultTotalHeader;
    }

    public static class ScoringCalculatorRegistry
    {
        public const ScoringCalculatorType DefaultCalculatorType = ScoringCalculatorType.AppendixA;
        public const string DefaultTotalHeader = "Total";

        // Methods that apply the discard pattern to groups of races (see DiscardRaceGroups).
        public static readonly IReadOnlyList<DiscardMethod> PatternDiscardMethods = new[]
        {
            DiscardMethod.RaceCountPattern,
            DiscardMethod.PerDay,
            DiscardMethod.PerChildSeries
        };

        private static readonly ScoringFieldDescriptor ScoresToCountField = new(
            nameof(ScoringSystemSettings.ScoresToCount),
            "Scores to count",
            "Each boat's best this many scores make up its series total. The others show as discards.",
            ScoringFieldType.Integer,
            required: true);

        private static readonly IReadOnlyDictionary<ScoringCalculatorType, ScoringCalculatorRegistration> _registrations =
            new[]
            {
                Simple(ScoringCalculatorType.AppendixA, Pipeline(AppendixA.Standard)),
                Simple(ScoringCalculatorType.AppendixAPre2025, Pipeline(AppendixA.Pre2025)),
                Simple(ScoringCalculatorType.AppAAltFirstIsZero, Pipeline(AppendixA.FirstIsZero)),
                Simple(ScoringCalculatorType.AppAAltFirstIsPoint7, Pipeline(AppendixA.FirstIsPoint7)),
                Simple(ScoringCalculatorType.AppAAltFirstIsPoint75, Pipeline(AppendixA.FirstIsPoint75)),
                Simple(ScoringCalculatorType.LowPointAverageInclDnc, Pipeline(LowPointAverages.IncludingDnc)),
                Simple(ScoringCalculatorType.LowPointAverageExclDnc, Pipeline(LowPointAverages.ExcludingDnc)),
                Simple(ScoringCalculatorType.HighPointPercentage, Pipeline(HighPointPercentages.Create)),
                // Cox-Sprague chooses discards across the whole series, and Top X doesn't discard.
                Simple(ScoringCalculatorType.CoxSprague, Pipeline(CoxSprague.Create),
                    DiscardMethod.RaceCountPattern),
                Simple(ScoringCalculatorType.TopXHighPoint, Pipeline(TopX.Create),
                    DiscardMethod.RaceCountPattern),
                Simple(ScoringCalculatorType.PwaStandard, Pipeline(PwaStandard.Create)),
                // Keeps child series discards in a summary series; uses the discard pattern
                // in any other series.
                new ScoringCalculatorRegistration
                {
                    CalculatorType = ScoringCalculatorType.Pbg,
                    Create = Pipeline(LowPointAverages.Pbg),
                    AllowedDiscardMethods = new[] { DiscardMethod.ChildSeriesDiscardsOrPattern },
                    Columns = new[] { ScoringColumns.NetPoints, ScoringColumns.NetRaces },
                    TotalHeader = "Score"
                },
                new ScoringCalculatorRegistration
                {
                    CalculatorType = ScoringCalculatorType.Varc2026Shorthanded,
                    Create = Pipeline(TopX.Varc2026),
                    Fields = new[] { ScoresToCountField },
                    // Only the best scores count, so the discard pattern isn't used.
                    AllowedDiscardMethods = new[] { DiscardMethod.RaceCountPattern }
                },
                new ScoringCalculatorRegistration
                {
                    CalculatorType = ScoringCalculatorType.Varc2026,
                    Create = Pipeline(TopX.Varc2026),
                    Fields = new[]
                    {
                        ScoresToCountField,
                        new ScoringFieldDescriptor(
                            nameof(ScoringSystemSettings.MinimumCompetitors),
                            "Minimum boats",
                            "A race counts only when at least this many boats in the series came to the start. Its scores show as discards otherwise. Leave blank to count every race.",
                            ScoringFieldType.Integer,
                            required: false),
                        new ScoringFieldDescriptor(
                            nameof(ScoringSystemSettings.TiesStandAfterA81),
                            "Ties stand after A8.1",
                            "Yes: a tie remaining after RRS A8.1 stands. No: it's broken by the last race (A8.2).",
                            ScoringFieldType.Boolean,
                            required: false)
                    },
                    AllowedDiscardMethods = new[] { DiscardMethod.RaceCountPattern }
                },
                new ScoringCalculatorRegistration
                {
                    CalculatorType = ScoringCalculatorType.Wawasee2026,
                    Create = Pipeline(Wawasee2026.Create),
                    Fields = new[]
                    {
                        new ScoringFieldDescriptor(
                            nameof(ScoringSystemSettings.PenaltyThresholdPercent),
                            "Penalty threshold",
                            "Boats that sailed fewer than this percent of the races that count (rounded up to a whole race) get penalty points for each race short. Excused absences count as sailed.",
                            ScoringFieldType.Percent,
                            required: true),
                        new ScoringFieldDescriptor(
                            nameof(ScoringSystemSettings.PenaltyScoreValue),
                            "Penalty points",
                            "Points added for each race short of the penalty threshold. Usually last year's number of boats qualifying for awards, plus one.",
                            ScoringFieldType.Decimal,
                            required: true),
                        new ScoringFieldDescriptor(
                            nameof(ScoringSystemSettings.MinimumCompetitors),
                            "Minimum boats per day",
                            "A day's races count only when at least this many boats in the series started a race that day. Otherwise their scores show as discards. Leave blank to count every race.",
                            ScoringFieldType.Integer,
                            required: false)
                    },
                    // Only races that don't count are discarded.
                    AllowedDiscardMethods = new[] { DiscardMethod.RaceCountPattern },
                    Columns = new[]
                    {
                        ScoringColumns.Penalties,
                        ScoringColumns.AverageFinish,
                        ScoringColumns.AverageWithPenalties
                    },
                    TotalHeader = "Points (with Penalties)"
                }
            }.ToDictionary(r => r.CalculatorType);

        public static IEnumerable<ScoringCalculatorRegistration> All => _registrations.Values;

        // Null (an unset base system) uses the default calculator, as name matching did.
        public static ScoringCalculatorRegistration Get(ScoringCalculatorType? calculatorType)
        {
            return _registrations.TryGetValue(calculatorType ?? DefaultCalculatorType, out var registration)
                ? registration
                : _registrations[DefaultCalculatorType];
        }

        public static IScoringCalculator CreateCalculator(
            ScoringCalculatorType? calculatorType,
            ScoringSystem scoringSystem)
        {
            return Get(calculatorType).Create(scoringSystem);
        }

        // The standard columns followed by the calculator's own, in display order.
        public static IEnumerable<ScoringColumnDescriptor> GetColumns(ScoringCalculatorType? calculatorType)
        {
            return ScoringColumns.Standard.Concat(Get(calculatorType).Columns);
        }

        // Every column the edit form might show, for any calculator.
        public static IEnumerable<ScoringColumnDescriptor> AllColumns =>
            ScoringColumns.Standard.Concat(_registrations.Values.SelectMany(r => r.Columns))
                .GroupBy(c => c.Key)
                .Select(g => g.First());

        // Every field the edit form might show, for any calculator.
        public static IEnumerable<ScoringFieldDescriptor> AllFields =>
            _registrations.Values.SelectMany(r => r.Fields)
                .GroupBy(f => f.PropertyName)
                .Select(g => g.First());

        // The discard pattern applies to the whole series for RaceCountPattern, and to
        // each day's or child series' races for PerDay and PerChildSeries.
        public static bool UsesDiscardPattern(DiscardMethod discardMethod)
        {
            return PatternDiscardMethods.Contains(discardMethod)
                || discardMethod == DiscardMethod.ChildSeriesDiscardsOrPattern;
        }

        // Validates a system's own values combined with what it inherits from its parent.
        // parent may be null for a base system. Returns (property name, message) pairs;
        // property name is a ScoringSystem or ScoringSystemSettings property.
        public static IEnumerable<(string PropertyName, string Message)> GetValidationErrors(
            ScoringSystem system,
            ScoringSystem parent)
        {
            var calculatorType = parent?.CalculatorType ?? system.CalculatorType;
            var registration = Get(calculatorType);
            var effectiveMethod = system.DiscardMethod
                ?? parent?.EffectiveDiscardMethod
                ?? DiscardMethod.RaceCountPattern;
            var ownSettings = system.Settings ?? new ScoringSystemSettings();
            var effectiveSettings = ownSettings.MergeOver(parent?.EffectiveSettings);

            if (!registration.AllowedDiscardMethods.Contains(effectiveMethod))
            {
                yield return (nameof(ScoringSystem.DiscardMethod),
                    $"This scoring system type does not support the \"{effectiveMethod}\" discard method.");
            }

            if (UsesDiscardPattern(effectiveMethod)
                && !String.IsNullOrWhiteSpace(system.DiscardPattern))
            {
                var patternErrors = ScoringService.GetDiscardSequenceErrors(system.DiscardPattern).ToList();
                if (patternErrors.Count > 0)
                {
                    yield return (nameof(ScoringSystem.DiscardPattern),
                        $"Discard pattern is not valid: {String.Join("; ", patternErrors)}");
                }
            }

            foreach (var (key, visibility) in ownSettings.ColumnVisibilities
                ?? new Dictionary<string, ColumnVisibility?>())
            {
                if (visibility.HasValue && !Enum.IsDefined(visibility.Value))
                {
                    yield return (nameof(ScoringSystemSettings.ColumnVisibilities),
                        $"Column visibility for \"{key}\" is not valid.");
                }
            }

            foreach (var field in registration.Fields)
            {
                var value = effectiveSettings.GetValue(field.PropertyName);
                if (value == null)
                {
                    if (field.Required)
                    {
                        yield return (field.PropertyName, $"{field.Label} is required.");
                    }
                    continue;
                }
                if (field.FieldType == ScoringFieldType.Boolean)
                {
                    continue;
                }
                var number = Convert.ToDecimal(value);
                if (field.FieldType == ScoringFieldType.Percent && (number < 0m || number > 100m))
                {
                    yield return (field.PropertyName, $"{field.Label} must be between 0 and 100.");
                }
                else if (field.FieldType == ScoringFieldType.Integer && number < 0m)
                {
                    yield return (field.PropertyName, $"{field.Label} cannot be negative.");
                }
            }
        }

        // Drops settings and column visibilities the system's calculator doesn't use, so
        // stale values don't linger after the parent system is changed.
        public static ScoringSystemSettings RemoveUnusedSettings(
            ScoringSystemSettings settings,
            ScoringCalculatorType? calculatorType)
        {
            if (settings == null)
            {
                return new ScoringSystemSettings();
            }
            return settings.KeepOnly(
                Get(calculatorType).Fields.Select(f => f.PropertyName),
                GetColumns(calculatorType).Select(c => c.Key));
        }

        // A system scored by ScoringPipeline with the recipe from Scoring/Systems.
        private static Func<ScoringSystem, IScoringCalculator> Pipeline(Func<ScoringSystem, ScoringRecipe> recipe)
        {
            return system => new ScoringPipeline(system, recipe(system));
        }

        private static ScoringCalculatorRegistration Simple(
            ScoringCalculatorType type,
            Func<ScoringSystem, IScoringCalculator> create,
            params DiscardMethod[] allowedDiscardMethods)
        {
            return new ScoringCalculatorRegistration
            {
                CalculatorType = type,
                Create = create,
                AllowedDiscardMethods = allowedDiscardMethods.Length > 0
                    ? allowedDiscardMethods
                    : PatternDiscardMethods
            };
        }
    }
}
