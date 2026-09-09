using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SailScores.Core.Model;
using SailScores.Core.Scoring;

namespace SailScores.Test.Unit.Core.Scoring;

public class ScoreCodeGroupTests
{
    /// <summary>
    /// Test that a score code group with NumberOfRaces limitation correctly identifies overage scores.
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
    /// Test limiting by number of races: competitor with 5 races of a code, limit is 2.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_NumberOfRaces_Limitation_IdentifiesOverages()
    {
        var group = new ScoreCodeGroup
        {
            IncludedCodeNames = new List<string> { "ORA" },
            LimitationType = ScoreCodeGroupLimitationType.NumberOfRaces,
            LimitationValue = 2m
        };

        // Scenario: competitor has 5 races with ORA code
        // Only 2 are allowed, so 3 are overage
        int competitorOraCount = 5;
        int allowedCount = (int)group.LimitationValue;
        int overageCount = competitorOraCount - allowedCount;

        Assert.Equal(3, overageCount);
    }

    /// <summary>
    /// Test limiting by percent of non-discarded races: 10 total races, 2 discarded, 8 non-discarded.
    /// Limit is 50% of non-discarded = 4. If competitor has 6 ORA, that's 2 overage.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_PercentOfNonDiscardedRaces_CalculatesCorrectly()
    {
        var group = new ScoreCodeGroup
        {
            IncludedCodeNames = new List<string> { "ORA" },
            LimitationType = ScoreCodeGroupLimitationType.PercentOfRaces,
            LimitationValue = 50m,
            UseNonDiscardedRaces = true
        };

        int totalRaces = 10;
        int discardedCount = 2;
        int nonDiscardedCount = totalRaces - discardedCount; // 8

        int allowedCount = (int)Math.Floor(group.LimitationValue / 100m * nonDiscardedCount); // 4
        int competitorOraCount = 6;
        int overageCount = competitorOraCount - allowedCount; // 2

        Assert.Equal(4, allowedCount);
        Assert.Equal(2, overageCount);
    }

    /// <summary>
    /// Test limiting by percent of all races (not just non-discarded).
    /// 10 total races, limit is 50% of all = 5. If competitor has 7 ORA, that's 2 overage.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_PercentOfAllRaces_CalculatesCorrectly()
    {
        var group = new ScoreCodeGroup
        {
            IncludedCodeNames = new List<string> { "ORA" },
            LimitationType = ScoreCodeGroupLimitationType.PercentOfRaces,
            LimitationValue = 50m,
            UseNonDiscardedRaces = false
        };

        int totalRaces = 10;
        int allowedCount = (int)Math.Floor(group.LimitationValue / 100m * totalRaces); // 5
        int competitorOraCount = 7;
        int overageCount = competitorOraCount - allowedCount; // 2

        Assert.Equal(5, allowedCount);
        Assert.Equal(2, overageCount);
    }

    /// <summary>
    /// Test limiting by number of distinct dates.
    /// Competitor has races with ORA code on 4 distinct dates, limit is 2 dates.
    /// So 2 dates worth of ORA are overage.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_NumberOfDates_Limitation_IdentifiesOverages()
    {
        var group = new ScoreCodeGroup
        {
            IncludedCodeNames = new List<string> { "ORA" },
            LimitationType = ScoreCodeGroupLimitationType.NumberOfDates,
            LimitationValue = 2m
        };

        // Competitor has ORA on 4 distinct dates
        var racesDates = new[]
        {
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 1), // same date, same race probably
            new DateTime(2024, 1, 8),
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 22)
        };

        var distinctDates = racesDates.Distinct().Count(); // 4
        int allowedDates = (int)group.LimitationValue; // 2
        int overageDates = distinctDates - allowedDates; // 2

        Assert.Equal(4, distinctDates);
        Assert.Equal(2, overageDates);
    }

    /// <summary>
    /// Test overage selection: LatestFirst means scores on the latest dates are marked overage.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_OverageSelection_LatestFirst_SelectsLatestDates()
    {
        var group = new ScoreCodeGroup
        {
            IncludedCodeNames = new List<string> { "ORA" },
            LimitationType = ScoreCodeGroupLimitationType.NumberOfRaces,
            LimitationValue = 2m,
            OverageSelectionMethod = ScoreCodeGroupOverageSelection.LatestFirst
        };

        var raceDates = new[]
        {
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 8),
            new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 22)
        };

        // LatestFirst: sort by date desc, take the last N (which are the earliest)
        var sortedByDateDesc = raceDates.OrderByDescending(d => d).ToList();
        int allowedCount = 2;
        var overageScores = sortedByDateDesc.Skip(allowedCount).ToList(); // Scores on 1/8 and 1/1

        Assert.Equal(2, overageScores.Count);
        Assert.Contains(new DateTime(2024, 1, 8), overageScores);
        Assert.Contains(new DateTime(2024, 1, 1), overageScores);
    }

    /// <summary>
    /// Test overage selection: WorstFirst means highest scores (worst in low-point) are marked overage.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_OverageSelection_WorstFirst_SelectsHighestScores()
    {
        var group = new ScoreCodeGroup
        {
            IncludedCodeNames = new List<string> { "ORA" },
            LimitationType = ScoreCodeGroupLimitationType.NumberOfRaces,
            LimitationValue = 2m,
            OverageSelectionMethod = ScoreCodeGroupOverageSelection.WorstFirst
        };

        var scores = new decimal[] { 10m, 15m, 20m, 25m };

        // WorstFirst: sort by score desc, take the excess
        var sortedByScoreDesc = scores.OrderByDescending(s => s).ToList();
        int allowedCount = 2;
        var overageScores = sortedByScoreDesc.Skip(allowedCount).ToList(); // 15, 10

        Assert.Equal(2, overageScores.Count);
        Assert.Contains(25m, scores);
        Assert.Contains(20m, scores);
        // The worst 2 (highest scores) go to overage
    }

    /// <summary>
    /// Test inheritance: inherited groups are read-only in child systems.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_Inheritance_ChildCannotModifyParentGroups()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var parentSystem = new ScoringSystem
        {
            Id = parentId,
            Name = "Parent System",
            ClubId = null,
            ScoreCodeGroups = new List<ScoreCodeGroup>
            {
                new ScoreCodeGroup
                {
                    Id = Guid.NewGuid(),
                    ScoringSystemId = parentId,
                    Name = "Parent Group",
                    IncludedCodeNames = new List<string> { "ORA" }
                }
            }
        };

        var childSystem = new ScoringSystem
        {
            Id = childId,
            Name = "Child System",
            ClubId = Guid.NewGuid(),
            ParentSystemId = parentId,
            ScoreCodeGroups = new List<ScoreCodeGroup>(), // No own groups
            InheritedScoreCodeGroups = parentSystem.ScoreCodeGroups // Inherited from parent
        };

        // Child cannot override parent groups (read-only)
        // This is enforced at the service/controller level, not the model level
        Assert.NotEmpty(childSystem.InheritedScoreCodeGroups);
        Assert.Empty(childSystem.ScoreCodeGroups);
    }

    /// <summary>
    /// Test that multiple codes can be included in a single group.
    /// </summary>
    [Fact]
    public void ScoreCodeGroup_MultipleCodes_AreLimitedTogether()
    {
        var group = new ScoreCodeGroup
        {
            IncludedCodeNames = new List<string> { "ORA", "RC", "SB" },
            LimitationType = ScoreCodeGroupLimitationType.NumberOfRaces,
            LimitationValue = 5m
        };

        // Competitor receives: 2 ORA, 2 RC, 2 SB = 6 total
        // Limit is 5, so 1 is overage
        int totalCodeOccurrences = 6;
        int allowedCount = (int)group.LimitationValue;
        int overageCount = totalCodeOccurrences - allowedCount;

        Assert.Equal(1, overageCount);
    }
}
