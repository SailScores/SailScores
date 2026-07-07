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
}
