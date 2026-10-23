using System;
using System.Collections.Generic;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using Xunit;

namespace SailScores.Test.Unit.Web.Models;

public class ResultsTableViewModelTests
{
    // Two races on May 1, then one on May 8.
    private static ResultsTableViewModel MakeTable(ResultsColumnSeparator separators,
        ColumnVisibility raceColumns = ColumnVisibility.Always) => new()
    {
        Results = new FlatResults
        {
            Races = new List<FlatRace>
            {
                new() { Id = Guid.NewGuid(), Date = new DateTime(2026, 5, 1, 10, 0, 0) },
                new() { Id = Guid.NewGuid(), Date = new DateTime(2026, 5, 1, 14, 0, 0) },
                new() { Id = Guid.NewGuid(), Date = new DateTime(2026, 5, 8) },
            }
        },
        ColumnSeparators = separators,
        RaceColumnsVisibility = raceColumns
    };

    [Fact]
    public void HasSeparatorBefore_None_NeverSeparates()
    {
        var table = MakeTable(ResultsColumnSeparator.None);

        Assert.False(table.HasSeparatorBefore(1));
        Assert.False(table.HasSeparatorBefore(2));
    }

    [Fact]
    public void HasSeparatorBefore_EveryColumn_SeparatesAllButFirstColumn()
    {
        var table = MakeTable(ResultsColumnSeparator.EveryColumn);

        Assert.False(table.HasSeparatorBefore(0));
        Assert.True(table.HasSeparatorBefore(1));
        Assert.True(table.HasSeparatorBefore(2));
    }

    [Fact]
    public void HasSeparatorBefore_RaceDays_SeparatesOnlyWhenDateChanges()
    {
        var table = MakeTable(ResultsColumnSeparator.RaceDays);

        Assert.False(table.HasSeparatorBefore(0));
        Assert.False(table.HasSeparatorBefore(1));
        Assert.True(table.HasSeparatorBefore(2));
    }

    [Fact]
    public void HasSeparatorBefore_IndexPastLastRace_ReturnsFalse()
    {
        var table = MakeTable(ResultsColumnSeparator.EveryColumn);

        Assert.False(table.HasSeparatorBefore(3));
    }

    [Fact]
    public void RaceColumnCssClass_OnLargerScreensWithSeparator_CombinesClasses()
    {
        var table = MakeTable(ResultsColumnSeparator.EveryColumn, ColumnVisibility.OnLargerScreens);

        Assert.Equal("d-none d-sm-table-cell", table.RaceColumnCssClass(0));
        Assert.Equal("d-none d-sm-table-cell results-separator", table.RaceColumnCssClass(1));
    }

    [Fact]
    public void RaceColumnCssClass_AlwaysWithoutSeparator_IsEmpty()
    {
        var table = MakeTable(ResultsColumnSeparator.None);

        Assert.Equal(string.Empty, table.RaceColumnCssClass(1));
    }

    [Fact]
    public void ShowRaceColumns_Hidden_IsFalse()
    {
        Assert.False(MakeTable(ResultsColumnSeparator.None, ColumnVisibility.Hidden).ShowRaceColumns);
        Assert.True(MakeTable(ResultsColumnSeparator.None, ColumnVisibility.OnLargerScreens).ShowRaceColumns);
    }

    [Fact]
    public void WithTemplate_CopiesDisplaySettings()
    {
        var template = new ResolvedTemplate
        {
            SailNumberVisibility = ColumnVisibility.Hidden,
            CompetitorNameVisibility = ColumnVisibility.OnLargerScreens,
            CompetitorNameHeader = "Skipper",
            BoatNameVisibility = ColumnVisibility.Always,
            BoatNameHeader = "Yacht",
            CompetitorClubVisibility = ColumnVisibility.Always,
            ShowPreDiscardTotal = true,
            HighlightTopThree = false,
            ColumnSeparators = ResultsColumnSeparator.RaceDays,
            RaceColumnsVisibility = ColumnVisibility.Hidden,
        };

        var table = new ResultsTableViewModel().WithTemplate(template);

        Assert.Equal(ColumnVisibility.Hidden, table.SailNumberVisibility);
        Assert.Equal(ColumnVisibility.OnLargerScreens, table.CompetitorNameVisibility);
        Assert.Equal("Skipper", table.CompetitorNameHeader);
        Assert.Equal(ColumnVisibility.Always, table.BoatNameVisibility);
        Assert.Equal("Yacht", table.BoatNameHeader);
        Assert.Equal(ColumnVisibility.Always, table.CompetitorClubVisibility);
        Assert.True(table.ShowPreDiscardTotal);
        Assert.False(table.HighlightTopThree);
        Assert.Equal(ResultsColumnSeparator.RaceDays, table.ColumnSeparators);
        Assert.Equal(ColumnVisibility.Hidden, table.RaceColumnsVisibility);
    }
}
