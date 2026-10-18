using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring;

public class ScoringColumnsTests
{
    private static FlatSeriesScore MakeScore(
        decimal? totalIncludingDiscards = null,
        decimal? participation = null,
        params decimal[] raceScores) => new()
    {
        CompetitorId = Guid.NewGuid(),
        TotalScoreIncludingDiscards = totalIncludingDiscards,
        ParticipationPercent = participation,
        Scores = raceScores.Select(v => new FlatCalculatedScore { ScoreValue = v }).ToList()
    };

    [Fact]
    public void Resolve_StandardColumns_FillsValuesFromScoreProperties()
    {
        var scores = new List<FlatSeriesScore> { MakeScore(12m, 75m) };

        var columns = ScoringColumns.Resolve(ScoringColumns.Standard, null, false, scores);

        Assert.Equal(
            new[] { ScoringColumns.PreDiscardTotalKey, ScoringColumns.ParticipationKey },
            columns.Select(c => c.Key));
        Assert.All(columns, c => Assert.Equal(ColumnVisibility.Hidden, c.Visibility));
        Assert.Equal(12m, scores[0].ColumnValues[ScoringColumns.PreDiscardTotalKey].Value);
        Assert.Equal(75m, scores[0].ColumnValues[ScoringColumns.ParticipationKey].Value);
    }

    [Fact]
    public void Resolve_PercentSystem_LeavesOutPreDiscardTotal()
    {
        var scores = new List<FlatSeriesScore> { MakeScore(12m, 75m) };

        var columns = ScoringColumns.Resolve(ScoringColumns.Standard, null, true, scores);

        Assert.Equal(new[] { ScoringColumns.ParticipationKey }, columns.Select(c => c.Key));
        Assert.False(scores[0].ColumnValues.ContainsKey(ScoringColumns.PreDiscardTotalKey));
    }

    [Fact]
    public void Resolve_SettingsVisibility_OverridesDefault()
    {
        var settings = new ScoringSystemSettings();
        settings.ColumnVisibilities[ScoringColumns.ParticipationKey] = ColumnVisibility.OnLargerScreens;

        var columns = ScoringColumns.Resolve(
            ScoringColumns.Standard, settings, false, new List<FlatSeriesScore> { MakeScore() });

        Assert.Equal(ColumnVisibility.OnLargerScreens,
            columns.Single(c => c.Key == ScoringColumns.ParticipationKey).Visibility);
        Assert.Equal(ColumnVisibility.Hidden,
            columns.Single(c => c.Key == ScoringColumns.PreDiscardTotalKey).Visibility);
    }

    [Fact]
    public void Resolve_OnlyWhenAnyValueWithNoValues_LeavesColumnOut()
    {
        var scores = new List<FlatSeriesScore> { MakeScore() };

        var columns = ScoringColumns.Resolve(new[] { ScoringColumns.Penalties }, null, true, scores);

        Assert.Empty(columns);
    }

    [Fact]
    public void Resolve_OnlyWhenAnyValueWithAValue_IncludesColumn()
    {
        var penalized = MakeScore();
        penalized.ColumnValues = new Dictionary<string, ScoringColumnValue>
        {
            [ScoringColumns.PenaltiesKey] = new() { Value = 2m, Multiplier = 1.5m }
        };
        var scores = new List<FlatSeriesScore> { MakeScore(), penalized };

        var columns = ScoringColumns.Resolve(new[] { ScoringColumns.Penalties }, null, true, scores);

        var column = Assert.Single(columns);
        Assert.Equal(ColumnVisibility.Always, column.Visibility);
    }

    [Fact]
    public void GetColumns_Wawasee2026_AddsCalculatorColumnsAfterStandard()
    {
        var keys = ScoringCalculatorRegistry.GetColumns(ScoringCalculatorType.Wawasee2026).Select(c => c.Key);

        Assert.Equal(
            new[]
            {
                ScoringColumns.PreDiscardTotalKey, ScoringColumns.ParticipationKey,
                ScoringColumns.PenaltiesKey, ScoringColumns.AverageFinishKey,
                ScoringColumns.AverageWithPenaltiesKey
            },
            keys);
    }

    [Fact]
    public void AddLegacyColumns_NoColumns_AddsStandardWithPreDiscardFallback()
    {
        var results = new FlatResults
        {
            CalculatedScores = new List<FlatSeriesScore> { MakeScore(null, 50m, 1m, 2m, 3m) }
        };

        ScoringColumns.AddLegacyColumns(results);

        Assert.Equal(2, results.Columns.Count);
        var score = results.CalculatedScores.Single();
        Assert.Equal(6m, score.ColumnValues[ScoringColumns.PreDiscardTotalKey].Value);
        Assert.Equal(50m, score.ColumnValues[ScoringColumns.ParticipationKey].Value);
    }

    [Fact]
    public void AddLegacyColumns_ExistingColumns_LeavesThemAlone()
    {
        var existing = new List<FlatResultColumn>();
        var results = new FlatResults
        {
            Columns = existing,
            CalculatedScores = new List<FlatSeriesScore> { MakeScore(5m) }
        };

        ScoringColumns.AddLegacyColumns(results);

        Assert.Same(existing, results.Columns);
        Assert.Null(results.CalculatedScores.Single().ColumnValues);
    }
}
