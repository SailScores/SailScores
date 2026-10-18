using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Db = SailScores.Database.Entities;

namespace SailScores.Test.Unit.Core.Scoring;

public class ScoringSystemSettingsTests
{
    [Fact]
    public void ToJson_EmptySettings_ReturnsNull()
    {
        Assert.Null(ScoringSystemSettings.ToJson(new ScoringSystemSettings()));
        Assert.Null(ScoringSystemSettings.ToJson(null));
    }

    [Fact]
    public void FromJson_RoundTrip_PreservesValuesAndOmitsNulls()
    {
        var settings = new ScoringSystemSettings { PenaltyScoreValue = 8.5m };

        var json = ScoringSystemSettings.ToJson(settings);
        var result = ScoringSystemSettings.FromJson(json);

        Assert.DoesNotContain(nameof(ScoringSystemSettings.PenaltyThresholdPercent), json);
        Assert.Equal(8.5m, result.PenaltyScoreValue);
        Assert.Null(result.PenaltyThresholdPercent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void FromJson_Blank_ReturnsEmptySettings(string json)
    {
        Assert.True(ScoringSystemSettings.FromJson(json).IsEmpty);
    }

    [Fact]
    public void MergeOver_ChildValueSet_OverridesParent()
    {
        var child = new ScoringSystemSettings { PenaltyScoreValue = 5m };
        var parent = new ScoringSystemSettings { PenaltyScoreValue = 8m, PenaltyThresholdPercent = 70m };

        var merged = child.MergeOver(parent);

        Assert.Equal(5m, merged.PenaltyScoreValue);
        Assert.Equal(70m, merged.PenaltyThresholdPercent);
    }

    [Fact]
    public void KeepOnly_RemovesOtherValues()
    {
        var settings = new ScoringSystemSettings { PenaltyScoreValue = 5m, PenaltyThresholdPercent = 70m };

        var result = settings.KeepOnly(new[] { nameof(ScoringSystemSettings.PenaltyThresholdPercent) });

        Assert.Null(result.PenaltyScoreValue);
        Assert.Equal(70m, result.PenaltyThresholdPercent);
    }

    [Fact]
    public void MergeOver_ColumnVisibilities_MergesByKey()
    {
        var child = new ScoringSystemSettings();
        child.ColumnVisibilities[ScoringColumns.PenaltiesKey] = ColumnVisibility.OnLargerScreens;
        child.ColumnVisibilities[ScoringColumns.ParticipationKey] = null;
        var parent = new ScoringSystemSettings();
        parent.ColumnVisibilities[ScoringColumns.PenaltiesKey] = ColumnVisibility.Hidden;
        parent.ColumnVisibilities[ScoringColumns.ParticipationKey] = ColumnVisibility.Always;

        var merged = child.MergeOver(parent);

        Assert.Equal(ColumnVisibility.OnLargerScreens, merged.GetColumnVisibility(ScoringColumns.PenaltiesKey));
        Assert.Equal(ColumnVisibility.Always, merged.GetColumnVisibility(ScoringColumns.ParticipationKey));
        Assert.Null(merged.GetColumnVisibility(ScoringColumns.PreDiscardTotalKey));
    }

    [Fact]
    public void ToJson_ColumnVisibilities_RoundTripsAndOmitsInherited()
    {
        var settings = new ScoringSystemSettings();
        settings.ColumnVisibilities[ScoringColumns.PenaltiesKey] = ColumnVisibility.Hidden;
        settings.ColumnVisibilities[ScoringColumns.ParticipationKey] = null;

        var json = ScoringSystemSettings.ToJson(settings);
        var result = ScoringSystemSettings.FromJson(json);

        Assert.DoesNotContain(ScoringColumns.ParticipationKey, json);
        Assert.Equal(ColumnVisibility.Hidden, result.GetColumnVisibility(ScoringColumns.PenaltiesKey));
        // The original keeps its entries.
        Assert.True(settings.ColumnVisibilities.ContainsKey(ScoringColumns.ParticipationKey));
    }

    [Fact]
    public void ToJson_OnlyInheritedColumnVisibilities_ReturnsNull()
    {
        var settings = new ScoringSystemSettings();
        settings.ColumnVisibilities[ScoringColumns.PenaltiesKey] = null;

        Assert.Null(ScoringSystemSettings.ToJson(settings));
    }

    [Fact]
    public void RemoveUnusedSettings_DropsColumnsTheCalculatorDoesNotProduce()
    {
        var settings = new ScoringSystemSettings();
        settings.ColumnVisibilities[ScoringColumns.PenaltiesKey] = ColumnVisibility.Hidden;
        settings.ColumnVisibilities[ScoringColumns.ParticipationKey] = ColumnVisibility.Always;

        var result = ScoringCalculatorRegistry.RemoveUnusedSettings(settings, ScoringCalculatorType.AppendixA);

        Assert.Null(result.GetColumnVisibility(ScoringColumns.PenaltiesKey));
        Assert.Equal(ColumnVisibility.Always, result.GetColumnVisibility(ScoringColumns.ParticipationKey));
    }

    [Fact]
    public void GetValidationErrors_UndefinedColumnVisibility_ReturnsError()
    {
        var system = new ScoringSystem { CalculatorType = ScoringCalculatorType.AppendixA };
        system.Settings.ColumnVisibilities[ScoringColumns.ParticipationKey] = (ColumnVisibility)99;

        var errors = ScoringCalculatorRegistry.GetValidationErrors(system, null).ToList();

        Assert.Contains(errors, e => e.PropertyName == nameof(ScoringSystemSettings.ColumnVisibilities));
    }

    [Fact]
    public void ApplyInheritedValues_ThreeLevelChain_ResolvesNearestNonNullValues()
    {
        var root = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            CalculatorType = ScoringCalculatorType.Wawasee2026,
            DiscardMethod = DiscardMethod.PerDay,
            Settings = ScoringSystemSettings.ToJson(new ScoringSystemSettings
            {
                PenaltyThresholdPercent = 70m,
                PenaltyScoreValue = 7m
            })
        };
        var middle = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            ParentSystemId = root.Id,
            Settings = ScoringSystemSettings.ToJson(new ScoringSystemSettings
            {
                PenaltyScoreValue = 8m
            })
        };
        var leaf = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            ParentSystemId = middle.Id,
            Settings = ScoringSystemSettings.ToJson(new ScoringSystemSettings { PenaltyScoreValue = 9m })
        };
        var model = new ScoringSystem();

        ScoringService.ApplyInheritedValues(model, leaf, new List<Db.ScoringSystem> { middle, root });

        Assert.Equal(ScoringCalculatorType.Wawasee2026, model.CalculatorType);
        Assert.Null(model.DiscardMethod);
        Assert.Equal(DiscardMethod.PerDay, model.EffectiveDiscardMethod);
        Assert.Equal(9m, model.Settings.PenaltyScoreValue);
        Assert.Null(model.Settings.PenaltyThresholdPercent);
        Assert.Equal(9m, model.EffectiveSettings.PenaltyScoreValue);
        Assert.Equal(70m, model.EffectiveSettings.PenaltyThresholdPercent);
    }

    [Fact]
    public void ApplyInheritedValues_NoDiscardMethodInChain_DefaultsToRaceCountPattern()
    {
        var root = new Db.ScoringSystem { Id = Guid.NewGuid() };
        var model = new ScoringSystem();

        ScoringService.ApplyInheritedValues(model, root, new List<Db.ScoringSystem>());

        Assert.Equal(DiscardMethod.RaceCountPattern, model.EffectiveDiscardMethod);
        Assert.True(model.EffectiveSettings.IsEmpty);
    }

    [Fact]
    public void ApplyInheritedValues_BlankPatternAndParticipation_InheritNearestAncestorValues()
    {
        var root = new Db.ScoringSystem { Id = Guid.NewGuid(), DiscardPattern = "0,1", ParticipationPercent = 50m };
        var middle = new Db.ScoringSystem { Id = Guid.NewGuid(), ParentSystemId = root.Id, DiscardPattern = "0,0,0,1" };
        var leaf = new Db.ScoringSystem { Id = Guid.NewGuid(), ParentSystemId = middle.Id, DiscardPattern = " " };
        var model = new ScoringSystem { DiscardPattern = leaf.DiscardPattern };

        ScoringService.ApplyInheritedValues(model, leaf, new List<Db.ScoringSystem> { middle, root });

        Assert.Equal("0,0,0,1", model.EffectiveDiscardPattern);
        Assert.Equal(50m, model.EffectiveParticipationPercent);
        Assert.Null(model.ParticipationPercent);
    }

    [Fact]
    public void ApplyInheritedValues_OwnPatternAndParticipation_OverrideAncestors()
    {
        var root = new Db.ScoringSystem { Id = Guid.NewGuid(), DiscardPattern = "0,1", ParticipationPercent = 50m };
        var leaf = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(), ParentSystemId = root.Id, DiscardPattern = "0,0,1", ParticipationPercent = 0m
        };
        var model = new ScoringSystem { DiscardPattern = leaf.DiscardPattern, ParticipationPercent = 0m };

        ScoringService.ApplyInheritedValues(model, leaf, new List<Db.ScoringSystem> { root });

        Assert.Equal("0,0,1", model.EffectiveDiscardPattern);
        Assert.Equal(0m, model.EffectiveParticipationPercent);
    }

    [Fact]
    public void EffectiveValues_NotResolved_FallBackToOwnValues()
    {
        var model = new ScoringSystem { DiscardPattern = "0,1", ParticipationPercent = 25m };

        Assert.Equal("0,1", model.EffectiveDiscardPattern);
        Assert.Equal(25m, model.EffectiveParticipationPercent);
    }
}

public class ScoringCalculatorRegistryTests
{
    private static ScoringSystem PenaltyParent(ScoringSystemSettings effectiveSettings = null) => new()
    {
        Id = Guid.NewGuid(),
        CalculatorType = ScoringCalculatorType.Wawasee2026,
        EffectiveDiscardMethod = DiscardMethod.RaceCountPattern,
        EffectiveSettings = effectiveSettings ?? new ScoringSystemSettings
        {
            PenaltyThresholdPercent = 70m
        }
    };

    private static ScoringSystem AppendixAParent() => new()
    {
        Id = Guid.NewGuid(),
        CalculatorType = ScoringCalculatorType.AppendixA,
        EffectiveDiscardMethod = DiscardMethod.RaceCountPattern
    };

    [Fact]
    public void GetValidationErrors_ExistingCalculatorWithValidPattern_ReturnsNoErrors()
    {
        var system = new ScoringSystem { DiscardPattern = "0,1" };

        var errors = ScoringCalculatorRegistry.GetValidationErrors(system, AppendixAParent());

        Assert.Empty(errors);
    }

    [Fact]
    public void GetValidationErrors_RaceCountPatternWithBadPattern_ReturnsPatternError()
    {
        var system = new ScoringSystem { DiscardPattern = "0,a" };

        var errors = ScoringCalculatorRegistry.GetValidationErrors(system, AppendixAParent()).ToList();

        Assert.Contains(errors, e => e.PropertyName == nameof(ScoringSystem.DiscardPattern));
    }

    [Fact]
    public void GetValidationErrors_PatternWithBadPattern_ReturnsOnlyPatternError()
    {
        var system = new ScoringSystem
        {
            DiscardPattern = "0,a",
            Settings = new ScoringSystemSettings { PenaltyScoreValue = 8m }
        };

        var errors = ScoringCalculatorRegistry.GetValidationErrors(system, PenaltyParent()).ToList();

        var error = Assert.Single(errors);
        Assert.Equal(nameof(ScoringSystem.DiscardPattern), error.PropertyName);
    }

    [Fact]
    public void GetValidationErrors_PbgWithBadPattern_ReturnsPatternError()
    {
        var system = new ScoringSystem { DiscardPattern = "0,a" };
        var parent = new ScoringSystem
        {
            Id = Guid.NewGuid(),
            CalculatorType = ScoringCalculatorType.Pbg,
            EffectiveDiscardMethod = DiscardMethod.ChildSeriesDiscardsOrPattern
        };

        var errors = ScoringCalculatorRegistry.GetValidationErrors(system, parent).ToList();

        var error = Assert.Single(errors);
        Assert.Equal(nameof(ScoringSystem.DiscardPattern), error.PropertyName);
    }

    [Fact]
    public void GetValidationErrors_RequiredSettingMissingEverywhere_ReturnsError()
    {
        var system = new ScoringSystem { DiscardPattern = "0" };

        var errors = ScoringCalculatorRegistry.GetValidationErrors(system, PenaltyParent()).ToList();

        var error = Assert.Single(errors);
        Assert.Equal(nameof(ScoringSystemSettings.PenaltyScoreValue), error.PropertyName);
    }

    [Fact]
    public void GetValidationErrors_RequiredSettingInheritedFromParent_ReturnsNoErrors()
    {
        var system = new ScoringSystem { DiscardPattern = "0" };
        var parent = PenaltyParent(new ScoringSystemSettings
        {
            PenaltyThresholdPercent = 70m,
            PenaltyScoreValue = 8m
        });

        var errors = ScoringCalculatorRegistry.GetValidationErrors(system, parent);

        Assert.Empty(errors);
    }

    [Fact]
    public void GetValidationErrors_DiscardMethodNotAllowed_ReturnsError()
    {
        var system = new ScoringSystem { DiscardPattern = "0", DiscardMethod = DiscardMethod.PreserveChildSeriesDiscards };

        var errors = ScoringCalculatorRegistry.GetValidationErrors(system, AppendixAParent()).ToList();

        Assert.Contains(errors, e => e.PropertyName == nameof(ScoringSystem.DiscardMethod));
    }

    [Fact]
    public void GetValidationErrors_PercentOutOfRange_ReturnsError()
    {
        var system = new ScoringSystem
        {
            DiscardPattern = "0",
            Settings = new ScoringSystemSettings { PenaltyScoreValue = 8m, PenaltyThresholdPercent = 120m }
        };

        var errors = ScoringCalculatorRegistry.GetValidationErrors(system, PenaltyParent()).ToList();

        var error = Assert.Single(errors);
        Assert.Equal(nameof(ScoringSystemSettings.PenaltyThresholdPercent), error.PropertyName);
    }

    [Fact]
    public void RemoveUnusedSettings_ParentChangedToAppendixA_DropsPenaltySettings()
    {
        var settings = new ScoringSystemSettings { PenaltyScoreValue = 8m, PenaltyThresholdPercent = 70m };

        var result = ScoringCalculatorRegistry.RemoveUnusedSettings(
            settings, ScoringCalculatorType.AppendixA);

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void RemoveUnusedSettings_Wawasee2026_KeepsCalculatorFields()
    {
        var settings = new ScoringSystemSettings { PenaltyScoreValue = 8m, PenaltyThresholdPercent = 70m };

        var result = ScoringCalculatorRegistry.RemoveUnusedSettings(
            settings, ScoringCalculatorType.Wawasee2026);

        Assert.Equal(8m, result.PenaltyScoreValue);
        Assert.Equal(70m, result.PenaltyThresholdPercent);
    }

    [Fact]
    public void Get_EveryCalculatorType_HasRegistration()
    {
        foreach (var type in Enum.GetValues<ScoringCalculatorType>())
        {
            Assert.Equal(type, ScoringCalculatorRegistry.Get(type).CalculatorType);
        }
    }
}
