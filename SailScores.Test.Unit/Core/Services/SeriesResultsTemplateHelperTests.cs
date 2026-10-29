using System.Collections.Generic;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Services;
using Xunit;

namespace SailScores.Test.Unit.Core.Services;

public class SeriesResultsTemplateHelperTests
{
    [Fact]
    public void GetDefaultTemplate_NonRegatta_HidesCompetitorClubAndClubLogo()
    {
        var result = SeriesResultsTemplateHelper.GetDefaultTemplate(isRegatta: false);

        Assert.Equal(ColumnVisibility.Hidden, result.CompetitorClubVisibility);
        Assert.False(result.ShowClubLogo);
    }

    [Fact]
    public void GetDefaultTemplate_Regatta_ShowsCompetitorClubOnLargerScreensAndHidesClubLogo()
    {
        var result = SeriesResultsTemplateHelper.GetDefaultTemplate(isRegatta: true);

        Assert.Equal(ColumnVisibility.OnLargerScreens, result.CompetitorClubVisibility);
        Assert.False(result.ShowClubLogo);
    }

    [Fact]
    public void GetResolvedTemplate_NullTemplateNotRegatta_ReturnsNonRegattaDefault()
    {
        var result = SeriesResultsTemplateHelper.GetResolvedTemplate(null, isRegatta: false);

        Assert.Equal(ColumnVisibility.Hidden, result.CompetitorClubVisibility);
    }

    [Fact]
    public void GetResolvedTemplate_NullTemplateIsRegatta_ReturnsRegattaDefault()
    {
        // Regression test: GetResolvedTemplate(null) previously always delegated to
        // GetDefaultTemplate() with no argument, silently ignoring isRegatta and
        // hiding the competitor club column for regatta series with no template assigned.
        var result = SeriesResultsTemplateHelper.GetResolvedTemplate(null, isRegatta: true);

        Assert.Equal(ColumnVisibility.OnLargerScreens, result.CompetitorClubVisibility);
    }

    [Fact]
    public void GetResolvedTemplate_FallbackTemplateUsedWhenPrimaryTemplateMissing_ReturnsFallbackValues()
    {
        var fallback = new SeriesResultsTemplate
        {
            CompetitorNameHeader = "Skipper",
            BoatNameHeader = "Hull",
            CompetitorClubVisibility = ColumnVisibility.Hidden,
            PreDiscardTotalVisibility = ColumnVisibility.Always,
            ParticipationVisibility = ColumnVisibility.OnLargerScreens,
            TotalsSeparator = true,
            ShowClubLogo = true
        };

        var result = SeriesResultsTemplateHelper.GetResolvedTemplate(null, isRegatta: true, fallbackTemplate: fallback);

        Assert.Equal("Skipper", result.CompetitorNameHeader);
        Assert.Equal("Hull", result.BoatNameHeader);
        Assert.Equal(ColumnVisibility.Hidden, result.CompetitorClubVisibility);
        Assert.Equal(ColumnVisibility.Always, result.PreDiscardTotalVisibility);
        Assert.Equal(ColumnVisibility.OnLargerScreens, result.ParticipationVisibility);
        Assert.True(result.TotalsSeparator);
        Assert.True(result.ShowClubLogo);
    }

    [Fact]
    public void GetResolvedTemplate_TemplateWithShowClubLogoTrue_ReturnsShowClubLogoTrue()
    {
        var template = new SeriesResultsTemplate
        {
            ShowClubLogo = true
        };

        var result = SeriesResultsTemplateHelper.GetResolvedTemplate(template);

        Assert.True(result.ShowClubLogo);
    }

    [Fact]
    public void GetResolvedTemplate_TemplateWithShowClubLogoFalse_ReturnsShowClubLogoFalse()
    {
        var template = new SeriesResultsTemplate
        {
            ShowClubLogo = false
        };

        var result = SeriesResultsTemplateHelper.GetResolvedTemplate(template);

        Assert.False(result.ShowClubLogo);
    }

    [Fact]
    public void GetDefaultTemplate_HighlightsTopThreeAndShowsRaceColumns()
    {
        var result = SeriesResultsTemplateHelper.GetDefaultTemplate();

        Assert.True(result.HighlightTopThree);
        Assert.Equal(ResultsColumnSeparator.None, result.ColumnSeparators);
        Assert.Equal(ColumnVisibility.Always, result.RaceColumnsVisibility);
    }

    [Fact]
    public void GetResolvedTemplate_WithDisplayOptions_CopiesThem()
    {
        var template = new SeriesResultsTemplate
        {
            HighlightTopThree = false,
            ColumnSeparators = ResultsColumnSeparator.EveryColumn,
            RaceColumnsVisibility = ColumnVisibility.Hidden
        };

        var result = SeriesResultsTemplateHelper.GetResolvedTemplate(template);

        Assert.False(result.HighlightTopThree);
        Assert.Equal(ResultsColumnSeparator.EveryColumn, result.ColumnSeparators);
        Assert.Equal(ColumnVisibility.Hidden, result.RaceColumnsVisibility);
    }

    [Fact]
    public void GetResolvedTemplate_WithScoreCodes_LooksUpCodesIgnoringCase()
    {
        var template = new SeriesResultsTemplate
        {
            ScoreCodes = new List<SeriesResultsTemplateScoreCode>
            {
                new() { Code = "DNC", Display = ScoreCodeDisplay.Hidden }
            }
        };

        var result = SeriesResultsTemplateHelper.GetResolvedTemplate(template);

        Assert.Equal(ScoreCodeDisplay.Hidden, result.ScoreCodes["dnc"].Display);
    }

    [Fact]
    public void GetDefaultTemplate_HasNoScoreCodeSettings()
    {
        Assert.Empty(SeriesResultsTemplateHelper.GetDefaultTemplate().ScoreCodes);
    }

    [Fact]
    public void GetDefaultTemplate_ShowsWeatherAndHidesAlternativeSailNumber()
    {
        var result = SeriesResultsTemplateHelper.GetDefaultTemplate();

        Assert.True(result.ShowWeather);
        Assert.Equal(ColumnVisibility.Hidden, result.AlternativeSailNumberVisibility);
        Assert.Null(result.SailNumberHeader);
        Assert.Null(result.CompetitorNameHeader);
        Assert.Null(result.BoatNameHeader);
        Assert.Null(result.CompetitorClubHeader);
        Assert.Null(result.TotalHeader);
    }

    [Fact]
    public void GetResolvedTemplate_WithColumnHeadersAndWeather_CopiesThem()
    {
        var template = new SeriesResultsTemplate
        {
            SailNumberHeader = "Bow",
            AlternativeSailNumberVisibility = ColumnVisibility.Always,
            AlternativeSailNumberHeader = "Class #",
            CompetitorClubHeader = "Yacht Club",
            TotalHeader = "Points",
            ShowWeather = false
        };

        var result = SeriesResultsTemplateHelper.GetResolvedTemplate(template);

        Assert.Equal("Bow", result.SailNumberHeader);
        Assert.Equal(ColumnVisibility.Always, result.AlternativeSailNumberVisibility);
        Assert.Equal("Class #", result.AlternativeSailNumberHeader);
        Assert.Equal("Yacht Club", result.CompetitorClubHeader);
        Assert.Equal("Points", result.TotalHeader);
        Assert.False(result.ShowWeather);
    }
}
