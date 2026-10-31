using System;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;

namespace SailScores.Core.Scoring
{
    public enum ScoringColumnFormat
    {
        Decimal,
        Percent
    }

    // Describes one extra results column a calculator can produce (after the total), so the
    // results table, CSV export and scoring system edit form can handle it without knowing
    // which calculator it came from. Visibility can be overridden per scoring system.
    public class ScoringColumnDescriptor
    {
        public ScoringColumnDescriptor(
            string key,
            string header,
            string helpText,
            ScoringColumnFormat format,
            ColumnVisibility defaultVisibility,
            bool onlyWhenAnyValue = false,
            Func<FlatSeriesScore, ScoringColumnValue> valueFromScore = null)
        {
            Key = key;
            Header = header;
            HelpText = helpText;
            Format = format;
            DefaultVisibility = defaultVisibility;
            OnlyWhenAnyValue = onlyWhenAnyValue;
            ValueFromScore = valueFromScore;
        }

        // Stored in results JSON and in ScoringSystemSettings.ColumnVisibilities; don't rename.
        public string Key { get; }
        public string Header { get; }
        public string HelpText { get; }
        public ScoringColumnFormat Format { get; }
        public ColumnVisibility DefaultVisibility { get; }
        // Left out of the results when no competitor has a value.
        public bool OnlyWhenAnyValue { get; }
        // For columns built from FlatSeriesScore properties. When null, the calculator
        // writes the value to SeriesCompetitorResults.ColumnValues.
        public Func<FlatSeriesScore, ScoringColumnValue> ValueFromScore { get; }
    }
}
