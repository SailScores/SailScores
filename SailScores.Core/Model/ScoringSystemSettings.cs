using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SailScores.Api.Enumerations;

namespace SailScores.Core.Model
{
    // Calculator-specific scoring system settings, stored as JSON in ScoringSystem.Settings.
    // Every property is nullable: null means "inherit from the parent system".
    // ScoringCalculatorRegistry decides which properties each calculator uses.
    // ColumnVisibilities is handled separately from the scalar properties: it merges key by key.
    public class ScoringSystemSettings
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly IReadOnlyDictionary<string, PropertyInfo> _properties =
            typeof(ScoringSystemSettings)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite
                    && Nullable.GetUnderlyingType(p.PropertyType) != null)
                .ToDictionary(p => p.Name, StringComparer.Ordinal);

        // Competitors who sailed fewer than this percent of races get penalty scores
        // added until they reach it.
        public decimal? PenaltyThresholdPercent { get; set; }

        // Score value of each penalty added for low participation.
        public decimal? PenaltyScoreValue { get; set; }

        // Number of each competitor's best scores that count toward the series total.
        public int? ScoresToCount { get; set; }

        // A race counts only when at least this many boats came to the start.
        public int? MinimumCompetitors { get; set; }

        // When true, a tie remaining after RRS A8.1 stands (A8.2 isn't applied).
        public bool? TiesStandAfterA81 { get; set; }

        // Results column visibility by ScoringColumnDescriptor.Key; a missing or null
        // entry inherits from the parent, then the column's default.
        public Dictionary<string, ColumnVisibility?> ColumnVisibilities { get; set; } = new();

        // Scalar setting names; doesn't include ColumnVisibilities.
        public static IEnumerable<string> PropertyNames => _properties.Keys;

        [JsonIgnore]
        public bool IsEmpty => _properties.Values.All(p => p.GetValue(this) == null)
            && !(ColumnVisibilities?.Values.Any(v => v.HasValue) ?? false);

        public ColumnVisibility? GetColumnVisibility(string key)
        {
            return ColumnVisibilities != null && ColumnVisibilities.TryGetValue(key, out var visibility)
                ? visibility
                : null;
        }

        public object GetValue(string propertyName)
        {
            return _properties[propertyName].GetValue(this);
        }

        public void SetValue(string propertyName, object value)
        {
            _properties[propertyName].SetValue(this, value);
        }

        // Returns a new instance with this object's values, falling back to parent values
        // where this object's value is null.
        public ScoringSystemSettings MergeOver(ScoringSystemSettings parent)
        {
            var merged = new ScoringSystemSettings();
            foreach (var property in _properties.Values)
            {
                merged.SetValue(property.Name,
                    property.GetValue(this) ?? (parent == null ? null : property.GetValue(parent)));
            }
            var keys = (ColumnVisibilities?.Keys ?? Enumerable.Empty<string>())
                .Concat(parent?.ColumnVisibilities?.Keys ?? Enumerable.Empty<string>())
                .Distinct();
            foreach (var key in keys)
            {
                var visibility = GetColumnVisibility(key) ?? parent?.GetColumnVisibility(key);
                if (visibility.HasValue)
                {
                    merged.ColumnVisibilities[key] = visibility;
                }
            }
            return merged;
        }

        // Returns a copy with only the named properties and column visibilities kept.
        public ScoringSystemSettings KeepOnly(
            IEnumerable<string> propertyNames,
            IEnumerable<string> columnKeys = null)
        {
            var keep = new HashSet<string>(propertyNames ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            var result = new ScoringSystemSettings();
            foreach (var name in keep.Where(_properties.ContainsKey))
            {
                result.SetValue(name, GetValue(name));
            }
            foreach (var key in (columnKeys ?? Enumerable.Empty<string>()).Distinct())
            {
                var visibility = GetColumnVisibility(key);
                if (visibility.HasValue)
                {
                    result.ColumnVisibilities[key] = visibility;
                }
            }
            return result;
        }

        public static ScoringSystemSettings FromJson(string json)
        {
            if (String.IsNullOrWhiteSpace(json))
            {
                return new ScoringSystemSettings();
            }
            var settings = JsonSerializer.Deserialize<ScoringSystemSettings>(json, _jsonOptions)
                ?? new ScoringSystemSettings();
            settings.ColumnVisibilities ??= new();
            return settings;
        }

        // Null when there is nothing to store.
        public static string ToJson(ScoringSystemSettings settings)
        {
            if (settings == null || settings.IsEmpty)
            {
                return null;
            }
            // The copy drops inherited (null) visibilities; with none left the key is left out.
            var copy = settings.MergeOver(null);
            if (copy.ColumnVisibilities.Count == 0)
            {
                copy.ColumnVisibilities = null;
            }
            return JsonSerializer.Serialize(copy, _jsonOptions);
        }
    }
}
