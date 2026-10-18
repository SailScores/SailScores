using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SailScores.Core.Model;
using SailScores.Core.Scoring;

namespace SailScores.Test.Unit.Core.Scoring;

// The ScoreCodeGroup model. ScoreCodeGroupScoringTests covers limits applied by a calculator.
public class ScoreCodeGroupTests
{
    /// <summary>
    /// Test that DisplayName returns the name when one is set.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_DisplayName_ReturnsNameWhenProvided()
    {
        var group = new ScoreCodeGroup
        {
            Name = "Penalties",
            IncludedCodeNames = new List<string> { "ORA", "RC" }
        };

        Assert.Equal("Penalties", group.DisplayName);
    }

    /// <summary>
    /// Test that DisplayName falls back to joined code names when Name is empty.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_DisplayName_ReturnsJoinedCodesWhenNameEmpty()
    {
        var group = new ScoreCodeGroup
        {
            Name = null,
            IncludedCodeNames = new List<string> { "ORA", "RC", "SB" }
        };

        var expected = "ORA, RC, SB";
        Assert.Equal(expected, group.DisplayName);
    }

    /// <summary>
    /// Test that DisplayName returns "(No codes)" when IncludedCodeNames is empty.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_DisplayName_ReturnsNoCodes_WhenNoCodesIncluded()
    {
        var group = new ScoreCodeGroup
        {
            Name = null,
            IncludedCodeNames = new List<string>()
        };

        Assert.Equal("(No codes)", group.DisplayName);
    }

    /// <summary>
    /// Test constructor properly initializes all fields.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_Constructor_InitializesAllProperties()
    {
        var codeName = new List<string> { "ORA", "RC" };
        var group = new ScoreCodeGroup(
            id: Guid.NewGuid(),
            scoringSystemId: Guid.NewGuid(),
            name: "Test Group",
            limitationType: ScoreCodeGroupLimitationType.PercentOfRaces,
            limitationValue: 50m,
            useNonDiscardedRaces: false,
            overageCodeName: "DNC",
            overageSelectionMethod: ScoreCodeGroupOverageSelection.WorstFirst,
            includedCodeNames: codeName);

        Assert.Equal("Test Group", group.Name);
        Assert.Equal(ScoreCodeGroupLimitationType.PercentOfRaces, group.LimitationType);
        Assert.Equal(50m, group.LimitationValue);
        Assert.False(group.UseNonDiscardedRaces);
        Assert.Equal("DNC", group.OverageCodeName);
        Assert.Equal(ScoreCodeGroupOverageSelection.WorstFirst, group.OverageSelectionMethod);
        Assert.Equal(2, group.IncludedCodeNames.Count);
    }

    /// <summary>
    /// Test that default values are set correctly.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_Defaults_AreCorrect()
    {
        var group = new ScoreCodeGroup();

        Assert.True(group.UseNonDiscardedRaces);
        Assert.Equal("DNC", group.OverageCodeName);
        Assert.Equal(ScoreCodeGroupOverageSelection.LatestFirst, group.OverageSelectionMethod);
    }

    /// <summary>
    /// Test that UseNonDiscardedRaces affects percent calculations appropriately.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_UseNonDiscardedRaces_Flag_IsPersisted()
    {
        var groupUsingNonDiscarded = new ScoreCodeGroup
        {
            LimitationType = ScoreCodeGroupLimitationType.PercentOfRaces,
            UseNonDiscardedRaces = true
        };

        var groupUsingAll = new ScoreCodeGroup
        {
            LimitationType = ScoreCodeGroupLimitationType.PercentOfRaces,
            UseNonDiscardedRaces = false
        };

        Assert.True(groupUsingNonDiscarded.UseNonDiscardedRaces);
        Assert.False(groupUsingAll.UseNonDiscardedRaces);
    }

    /// <summary>
    /// Test AllowedCountInfo factory methods for creating race-based and date-based info.
    /// </summary>
    [Fact]
    public void AllowedCountInfo_RaceBased_CreatesCorrectObject()
    {
        var info = AllowedCountInfo.RaceBased(5);

        Assert.False(info.IsDateBased);
        Assert.Equal(5, info.AllowedCount);
    }

    /// <summary>
    /// Test AllowedCountInfo for date-based limitations.
    /// </summary>
    [Fact]
    public void AllowedCountInfo_DateBased_CreatesCorrectObject()
    {
        var info = AllowedCountInfo.DateBased(3, ScoreCodeGroupOverageSelection.WorstFirst);

        Assert.True(info.IsDateBased);
        Assert.Equal(3, info.AllowedCount);
        Assert.Equal(ScoreCodeGroupOverageSelection.WorstFirst, info.OverageSelectionMethod);
    }
}
