using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using SailScores.Web.Extensions;
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
    public void HasSeparatorBefore_TotalsSeparator_SeparatesFirstColumnOnly()
    {
        var table = MakeTable(ResultsColumnSeparator.None);
        table.TotalsSeparator = true;

        Assert.True(table.HasSeparatorBefore(0));
        Assert.False(table.HasSeparatorBefore(1));
        Assert.Equal("results-separator", table.RaceColumnCssClass(0));
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
    public void HasSeparatorBefore_ChildSeries_SeparatesWhereChildSeriesChanges()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var table = new ResultsTableViewModel
        {
            Results = new FlatResults
            {
                Races = new List<FlatRace>
                {
                    new() { Id = Guid.NewGuid(), ChildSeriesId = first },
                    new() { Id = Guid.NewGuid(), ChildSeriesId = first },
                    new() { Id = Guid.NewGuid(), ChildSeriesId = second },
                    // A child series shown as one column, saved before ChildSeriesId existed.
                    new() { Id = Guid.NewGuid(), IsSeries = true },
                }
            },
            ColumnSeparators = ResultsColumnSeparator.ChildSeries
        };

        Assert.False(table.HasSeparatorBefore(1));
        Assert.True(table.HasSeparatorBefore(2));
        Assert.True(table.HasSeparatorBefore(3));
    }

    [Fact]
    public void HasSeparatorBefore_ChildSeriesWithoutChildSeries_NeverSeparates()
    {
        var table = MakeTable(ResultsColumnSeparator.ChildSeries);

        Assert.False(table.HasSeparatorBefore(1));
        Assert.False(table.HasSeparatorBefore(2));
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

    // Results saved before the setting moved to views carry the scoring system's visibility;
    // the view's setting wins, so Always shows the column on small screens too.
    [Theory]
    [InlineData(ColumnVisibility.Always, "d-table-cell")]
    [InlineData(ColumnVisibility.OnLargerScreens, "d-none d-sm-table-cell")]
    public void VisibleScoringColumns_Participation_UsesViewVisibilityOverStored(
        ColumnVisibility viewSetting, string expectedCss)
    {
        var table = new ResultsTableViewModel
        {
            Results = new FlatResults
            {
                Columns = new List<FlatResultColumn>
                {
                    new()
                    {
                        Key = ScoringColumns.ParticipationKey,
                        Header = "Participation",
                        Visibility = viewSetting == ColumnVisibility.Always
                            ? ColumnVisibility.OnLargerScreens
                            : ColumnVisibility.Always
                    }
                }
            },
            ParticipationVisibility = viewSetting
        };

        var column = Assert.Single(table.VisibleScoringColumns);
        Assert.Equal(viewSetting, column.Visibility);
        Assert.Equal(expectedCss, column.Visibility.ToCssClass());
    }

    [Theory]
    [InlineData(7, ColumnVisibility.Always, false)]
    [InlineData(8, ColumnVisibility.Always, true)]
    [InlineData(8, ColumnVisibility.OnLargerScreens, true)]
    [InlineData(8, ColumnVisibility.Hidden, false)]
    public void UsesWideLayout_ManyRaces_WideOnlyWhenRaceColumnsShown(
        int raceCount, ColumnVisibility raceColumns, bool expected)
    {
        var results = new FlatResults
        {
            Races = Enumerable.Range(1, raceCount).Select(i => new FlatRace { Id = Guid.NewGuid() }).ToList()
        };

        Assert.Equal(expected, ResultsTableViewModel.UsesWideLayout(results, raceColumns));
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
            PreDiscardTotalVisibility = ColumnVisibility.Always,
            ParticipationVisibility = ColumnVisibility.OnLargerScreens,
            TotalsSeparator = true,
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
        Assert.Equal(ColumnVisibility.Always, table.PreDiscardTotalVisibility);
        Assert.Equal(ColumnVisibility.OnLargerScreens, table.ParticipationVisibility);
        Assert.True(table.TotalsSeparator);
        Assert.False(table.HighlightTopThree);
        Assert.Equal(ResultsColumnSeparator.RaceDays, table.ColumnSeparators);
        Assert.Equal(ColumnVisibility.Hidden, table.RaceColumnsVisibility);
    }

    [Fact]
    public void GetScoreCodeDisplay_FromTemplate_FindsCodeAndReturnsNullOtherwise()
    {
        var template = SeriesResultsTemplateHelper.GetResolvedTemplate(new SailScores.Core.Model.SeriesResultsTemplate
        {
            ScoreCodes = new List<SailScores.Core.Model.SeriesResultsTemplateScoreCode>
            {
                new() { Code = "DNC", Display = ScoreCodeDisplay.CodeOnly }
            }
        });

        var table = new ResultsTableViewModel().WithTemplate(template);

        Assert.Equal(ScoreCodeDisplay.CodeOnly, table.GetScoreCodeDisplay("dnc").Display);
        Assert.Null(table.GetScoreCodeDisplay("DNF"));
        Assert.Null(table.GetScoreCodeDisplay(null));
    }

    [Theory]
    [InlineData(true, "discardmuted")]
    [InlineData(false, "")]
    public void DiscardCssClass_FromSettings_MutesWhenFaded(bool faded, string expected)
    {
        var table = new ResultsTableViewModel { DiscardFaded = faded };

        Assert.Equal(expected, table.DiscardCssClass);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void DiscardParentheses_FromSettings_OnlyWithNoOtherStyling(
        bool strikethrough, bool faded, bool expected)
    {
        var table = new ResultsTableViewModel
        {
            DiscardStrikethrough = strikethrough,
            DiscardFaded = faded
        };

        Assert.Equal(expected, table.DiscardParentheses);
    }

    [Fact]
    public void WithTemplate_CopiesDiscardSettings()
    {
        var template = new ResolvedTemplate
        {
            DiscardStrikethrough = false,
            DiscardFaded = false
        };

        var table = new ResultsTableViewModel().WithTemplate(template);

        Assert.False(table.DiscardStrikethrough);
        Assert.False(table.DiscardFaded);
    }

    [Fact]
    public void WithTemplate_CopiesColumnHeadersAndWeather()
    {
        var template = new ResolvedTemplate
        {
            SailNumberHeader = "Bow",
            AlternativeSailNumberVisibility = ColumnVisibility.OnLargerScreens,
            AlternativeSailNumberHeader = "Class #",
            CompetitorClubHeader = "Yacht Club",
            TotalHeader = "Points",
            ShowWeather = false
        };

        var table = new ResultsTableViewModel().WithTemplate(template);

        Assert.Equal("Bow", table.SailNumberHeader);
        Assert.Equal(ColumnVisibility.OnLargerScreens, table.AlternativeSailNumberVisibility);
        Assert.Equal("Class #", table.AlternativeSailNumberHeader);
        Assert.Equal("Yacht Club", table.CompetitorClubHeader);
        Assert.Equal("Points", table.TotalHeader);
        Assert.False(table.ShowWeather);
    }

    [Theory]
    [InlineData(false, ColumnVisibility.Hidden, "123")]
    [InlineData(true, ColumnVisibility.Hidden, "ALT-9")]
    [InlineData(true, ColumnVisibility.Always, "123")]
    public void GetSailNumber_PreferAlternative_UsesAlternativeOnlyWithoutItsOwnColumn(
        bool preferAlternative, ColumnVisibility alternativeColumn, string expected)
    {
        var table = new ResultsTableViewModel
        {
            PreferAlternativeSailNumbers = preferAlternative,
            AlternativeSailNumberVisibility = alternativeColumn
        };

        var result = table.GetSailNumber(new FlatCompetitor { SailNumber = "123", AlternativeSailNumber = "ALT-9" });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetSailNumber_PreferAlternativeWithoutOne_UsesSailNumber()
    {
        var table = new ResultsTableViewModel { PreferAlternativeSailNumbers = true };

        Assert.Equal("123", table.GetSailNumber(new FlatCompetitor { SailNumber = "123" }));
    }

    [Theory]
    [InlineData(ColumnVisibility.OnLargerScreens, ColumnVisibility.Always, ColumnVisibility.Always, ColumnVisibility.Always, RepeatedCompetitorColumn.CompetitorName, "Ann")]
    [InlineData(ColumnVisibility.Hidden, ColumnVisibility.Always, ColumnVisibility.Always, ColumnVisibility.Always, RepeatedCompetitorColumn.SailNumber, "123")]
    [InlineData(ColumnVisibility.Hidden, ColumnVisibility.Hidden, ColumnVisibility.OnLargerScreens, ColumnVisibility.Always, RepeatedCompetitorColumn.BoatName, "Wave")]
    [InlineData(ColumnVisibility.Hidden, ColumnVisibility.Hidden, ColumnVisibility.Hidden, ColumnVisibility.Always, RepeatedCompetitorColumn.AlternativeSailNumber, "ALT-9")]
    [InlineData(ColumnVisibility.Hidden, ColumnVisibility.Hidden, ColumnVisibility.Hidden, ColumnVisibility.Hidden, RepeatedCompetitorColumn.None, null)]
    public void RepeatedColumn_CompetitorNameHidden_UsesFirstShownColumn(
        ColumnVisibility name, ColumnVisibility sail, ColumnVisibility boat, ColumnVisibility alternative,
        RepeatedCompetitorColumn expectedColumn, string expectedValue)
    {
        var table = new ResultsTableViewModel
        {
            CompetitorNameVisibility = name,
            SailNumberVisibility = sail,
            BoatNameVisibility = boat,
            AlternativeSailNumberVisibility = alternative
        };
        var competitor = new FlatCompetitor
        {
            Name = "Ann",
            SailNumber = "123",
            BoatName = "Wave",
            AlternativeSailNumber = "ALT-9"
        };

        Assert.Equal(expectedColumn, table.RepeatedColumn);
        Assert.Equal(expectedValue, table.GetRepeatedValue(competitor));
    }

    [Fact]
    public void GetRepeatedValue_SailNumberPreferringAlternative_UsesShownSailNumber()
    {
        var table = new ResultsTableViewModel
        {
            CompetitorNameVisibility = ColumnVisibility.Hidden,
            PreferAlternativeSailNumbers = true
        };

        var result = table.GetRepeatedValue(new FlatCompetitor { SailNumber = "123", AlternativeSailNumber = "ALT-9" });

        Assert.Equal("ALT-9", result);
    }
}
