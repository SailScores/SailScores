using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Core.Services.Interfaces;
using SailScores.Web.Controllers;
using SailScores.Web.Models.SailScores;
using Xunit;

namespace SailScores.Test.Unit.Web.Controllers;

public class SeriesResultsTemplateControllerTests
{
    private const string ClubInitials = "TST";
    private readonly Guid _clubId = Guid.NewGuid();
    private readonly Mock<ISeriesResultsTemplateService> _templateServiceMock = new();
    private readonly Mock<IClubService> _clubServiceMock = new();
    private readonly Mock<ICompetitorFieldService> _competitorFieldServiceMock = new();
    private readonly Mock<ISeriesService> _seriesServiceMock = new();
    private readonly Mock<SailScores.Web.Services.Interfaces.IClubService> _webClubServiceMock = new();
    private readonly SeriesResultsTemplateController _controller;

    public SeriesResultsTemplateControllerTests()
    {
        _clubServiceMock.Setup(s => s.GetClubId(ClubInitials)).ReturnsAsync(_clubId);
        _competitorFieldServiceMock.Setup(s => s.GetFieldDefinitionsAsync(_clubId))
            .ReturnsAsync(new List<CompetitorFieldDefinition>());
        _competitorFieldServiceMock.Setup(s => s.GetTemplateSelectionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<SeriesResultsTemplateCustomField>());
        _templateServiceMock.Setup(s => s.GetClubScoreCodesAsync(_clubId))
            .ReturnsAsync(new List<ScoreCode>());
        _controller = new SeriesResultsTemplateController(
            _templateServiceMock.Object,
            _clubServiceMock.Object,
            _competitorFieldServiceMock.Object,
            _seriesServiceMock.Object,
            _webClubServiceMock.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    private SeriesResultsTemplate SetupTemplate(Guid clubId)
    {
        var template = new SeriesResultsTemplate { Id = Guid.NewGuid(), ClubId = clubId, Name = "View" };
        _templateServiceMock.Setup(s => s.GetTemplateAsync(template.Id)).ReturnsAsync(template);
        return template;
    }

    [Fact]
    public async Task Index_ClubWithoutDefaults_SeedsAndShowsDefaults()
    {
        var seriesDefault = Guid.NewGuid();
        var regattaDefault = Guid.NewGuid();
        _clubServiceMock.SetupSequence(s => s.GetMinimalClub(_clubId))
            .ReturnsAsync(new Club { Id = _clubId })
            .ReturnsAsync(new Club
            {
                Id = _clubId,
                DefaultSeriesResultsTemplateId = seriesDefault,
                DefaultRegattaSeriesResultsTemplateId = regattaDefault
            });
        _templateServiceMock.Setup(s => s.GetTemplatesForClubAsync(_clubId))
            .ReturnsAsync(new List<SeriesResultsTemplate>());

        var result = await _controller.Index(ClubInitials);

        _templateServiceMock.Verify(s => s.SeedDefaultTemplatesAsync(_clubId), Times.Once);
        var vm = Assert.IsType<SeriesResultsTemplateListViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(seriesDefault, vm.DefaultSeriesResultsTemplateId);
        Assert.Equal(regattaDefault, vm.DefaultRegattaSeriesResultsTemplateId);
    }

    [Fact]
    public async Task SetDefaults_ValidSelection_SavesAndRedirectsToIndex()
    {
        var model = new SeriesResultsTemplateListViewModel
        {
            DefaultSeriesResultsTemplateId = Guid.NewGuid(),
            DefaultRegattaSeriesResultsTemplateId = Guid.NewGuid(),
            DefaultSummarySeriesResultsTemplateId = Guid.NewGuid()
        };

        var result = await _controller.SetDefaults(ClubInitials, model);

        _templateServiceMock.Verify(s => s.SetClubDefaultsAsync(
            _clubId, model.DefaultSeriesResultsTemplateId, model.DefaultRegattaSeriesResultsTemplateId,
            model.DefaultSummarySeriesResultsTemplateId),
            Times.Once);
        Assert.Equal(nameof(SeriesResultsTemplateController.Index),
            Assert.IsType<RedirectToActionResult>(result).ActionName);
    }

    [Fact]
    public async Task SetDefaults_InvalidModel_DoesNotSave()
    {
        _controller.ModelState.AddModelError(
            nameof(SeriesResultsTemplateListViewModel.DefaultSeriesResultsTemplateId), "Invalid");

        var result = await _controller.SetDefaults(ClubInitials, new SeriesResultsTemplateListViewModel());

        _templateServiceMock.Verify(s => s.SetClubDefaultsAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()),
            Times.Never);
        Assert.Equal(nameof(SeriesResultsTemplateController.Index),
            Assert.IsType<RedirectToActionResult>(result).ActionName);
    }

    [Fact]
    public async Task Edit_TemplateFromAnotherClub_ReturnsNotFound()
    {
        var template = SetupTemplate(Guid.NewGuid());

        var result = await _controller.Edit(ClubInitials, template.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditPost_TemplateFromAnotherClub_ReturnsNotFoundWithoutSaving()
    {
        var template = SetupTemplate(Guid.NewGuid());

        var result = await _controller.Edit(ClubInitials, template.Id,
            new SeriesResultsTemplateViewModel { Name = "Changed" });

        Assert.IsType<NotFoundResult>(result);
        _templateServiceMock.Verify(s => s.SaveTemplateAsync(It.IsAny<SeriesResultsTemplate>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfirmed_TemplateFromAnotherClub_ReturnsNotFoundWithoutDeleting()
    {
        var template = SetupTemplate(Guid.NewGuid());

        var result = await _controller.DeleteConfirmed(ClubInitials, template.Id);

        Assert.IsType<NotFoundResult>(result);
        _templateServiceMock.Verify(s => s.DeleteTemplateAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Edit_TemplateFromThisClub_ReturnsView()
    {
        var template = SetupTemplate(_clubId);
        _competitorFieldServiceMock.Setup(s => s.GetFieldDefinitionsAsync(_clubId))
            .ReturnsAsync(new List<CompetitorFieldDefinition>());
        _competitorFieldServiceMock.Setup(s => s.GetTemplateSelectionsAsync(template.Id))
            .ReturnsAsync(new List<SeriesResultsTemplateCustomField>());

        var result = await _controller.Edit(ClubInitials, template.Id);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_TemplateWithScoreCodes_ListsClubCodesAndKeepsSavedSettings()
    {
        var template = SetupTemplate(_clubId);
        template.ScoreCodes = new List<SeriesResultsTemplateScoreCode>
        {
            new() { Code = "DNC", Display = ScoreCodeDisplay.Hidden },
            new() { Code = "OLD", Display = ScoreCodeDisplay.CodeOnly, Faded = true }
        };
        _competitorFieldServiceMock.Setup(s => s.GetFieldDefinitionsAsync(_clubId))
            .ReturnsAsync(new List<CompetitorFieldDefinition>());
        _competitorFieldServiceMock.Setup(s => s.GetTemplateSelectionsAsync(template.Id))
            .ReturnsAsync(new List<SeriesResultsTemplateCustomField>());
        _templateServiceMock.Setup(s => s.GetClubScoreCodesAsync(_clubId))
            .ReturnsAsync(new List<ScoreCode>
            {
                new() { Name = "DNF", Description = "Did not finish" },
                new() { Name = "dnc", Description = "Did not come to the starting area" }
            });

        var result = await _controller.Edit(ClubInitials, template.Id);

        var vm = Assert.IsType<SeriesResultsTemplateViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(new[] { "DNC", "DNF", "OLD" }, vm.ScoreCodes.Select(c => c.Code));
        var dnc = vm.ScoreCodes.Single(c => c.Code == "DNC");
        Assert.Equal(ScoreCodeDisplay.Hidden, dnc.Display);
        Assert.Equal("Did not come to the starting area", dnc.Description);
        Assert.Equal(ScoreCodeDisplay.CodeAndPoints, vm.ScoreCodes.Single(c => c.Code == "DNF").Display);
        Assert.True(vm.ScoreCodes.Single(c => c.Code == "OLD").Faded);
    }

    [Fact]
    public async Task EditPost_WithScoreCodes_SavesThemOnTemplate()
    {
        var template = SetupTemplate(_clubId);
        _templateServiceMock.Setup(s => s.SaveTemplateAsync(It.IsAny<SeriesResultsTemplate>()))
            .ReturnsAsync(template);
        var model = new SeriesResultsTemplateViewModel
        {
            Name = "Sparse",
            ScoreCodes = new List<TemplateScoreCodeViewModel>
            {
                new() { Code = "DNC", Display = ScoreCodeDisplay.Hidden },
                new() { Code = "DNF", Faded = true }
            }
        };

        await _controller.Edit(ClubInitials, template.Id, model);

        _templateServiceMock.Verify(s => s.SaveTemplateAsync(It.Is<SeriesResultsTemplate>(t =>
            t.ScoreCodes.Count == 2
            && t.ScoreCodes.Any(c => c.Code == "DNC" && c.Display == ScoreCodeDisplay.Hidden)
            && t.ScoreCodes.Any(c => c.Code == "DNF" && c.Faded))), Times.Once);
    }

    [Fact]
    public async Task Edit_WithReset_ShowsDefaultSettingsKeepingName()
    {
        var template = SetupTemplate(_clubId);
        template.Name = "Busy";
        template.HighlightTopThree = false;
        template.DiscardFaded = false;
        template.ScoreCodes = new List<SeriesResultsTemplateScoreCode>
        {
            new() { Code = "DNC", Display = ScoreCodeDisplay.Hidden }
        };
        _competitorFieldServiceMock.Setup(s => s.GetFieldDefinitionsAsync(_clubId))
            .ReturnsAsync(new List<CompetitorFieldDefinition>
            {
                new() { Id = Guid.NewGuid(), Name = "Fleet" }
            });
        _templateServiceMock.Setup(s => s.GetClubScoreCodesAsync(_clubId))
            .ReturnsAsync(new List<ScoreCode> { new() { Name = "DNC" } });

        var result = await _controller.Edit(ClubInitials, template.Id, reset: true);

        var vm = Assert.IsType<SeriesResultsTemplateViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.True(vm.IsReset);
        Assert.Equal(template.Id, vm.Id);
        Assert.Equal("Busy", vm.Name);
        Assert.True(vm.HighlightTopThree);
        Assert.True(vm.DiscardFaded);
        Assert.Equal(ScoreCodeDisplay.CodeAndPoints, Assert.Single(vm.ScoreCodes).Display);
        Assert.False(Assert.Single(vm.CustomFieldSelections).Selected);
        _competitorFieldServiceMock.Verify(s => s.GetTemplateSelectionsAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Index_SummaryDefault_IsShownAndCountsAsClubDefault()
    {
        var summaryDefault = Guid.NewGuid();
        _clubServiceMock.Setup(s => s.GetMinimalClub(_clubId))
            .ReturnsAsync(new Club
            {
                Id = _clubId,
                DefaultSeriesResultsTemplateId = Guid.NewGuid(),
                DefaultRegattaSeriesResultsTemplateId = Guid.NewGuid(),
                DefaultSummarySeriesResultsTemplateId = summaryDefault
            });
        _templateServiceMock.Setup(s => s.GetTemplatesForClubAsync(_clubId))
            .ReturnsAsync(new List<SeriesResultsTemplate>());

        var result = await _controller.Index(ClubInitials);

        var vm = Assert.IsType<SeriesResultsTemplateListViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(summaryDefault, vm.DefaultSummarySeriesResultsTemplateId);
        Assert.True(vm.IsClubDefault(summaryDefault));
    }

    [Fact]
    public async Task DeleteConfirmed_SummaryDefault_DoesNotDelete()
    {
        var template = SetupTemplate(_clubId);
        _clubServiceMock.Setup(s => s.GetMinimalClub(_clubId))
            .ReturnsAsync(new Club { Id = _clubId, DefaultSummarySeriesResultsTemplateId = template.Id });

        await _controller.DeleteConfirmed(ClubInitials, template.Id);

        _templateServiceMock.Verify(s => s.DeleteTemplateAsync(It.IsAny<Guid>()), Times.Never);
    }

    private Series SetupSeries(Guid clubId)
    {
        var series = new Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Spring",
            FlatResults = new SailScores.Core.FlatModel.FlatResults()
        };
        _seriesServiceMock.Setup(s => s.GetOneSeriesAsync(series.Id)).ReturnsAsync(series);
        return series;
    }

    [Fact]
    public async Task Preview_ClubSeries_ShowsTableWithFormSettings()
    {
        var series = SetupSeries(_clubId);
        var fieldId = Guid.NewGuid();
        var model = new SeriesResultsTemplateViewModel
        {
            ParticipationVisibility = ColumnVisibility.Always,
            TotalsSeparator = true,
            DiscardFaded = false,
            CustomFieldSelections = new List<TemplateCustomFieldSelectionViewModel>
            {
                new() { FieldDefinitionId = fieldId, Selected = true, Visibility = ColumnVisibility.Always },
                new() { FieldDefinitionId = Guid.NewGuid(), Selected = false, Visibility = ColumnVisibility.Always }
            }
        };

        var result = await _controller.Preview(ClubInitials, series.Id, model);

        var table = Assert.IsType<ResultsTableViewModel>(Assert.IsType<PartialViewResult>(result).Model);
        Assert.Same(series.FlatResults, table.Results);
        Assert.Equal(ColumnVisibility.Always, table.ParticipationVisibility);
        Assert.True(table.TotalsSeparator);
        Assert.False(table.DiscardFaded);
        Assert.True(table.ExcludeCompetitorLinks);
        Assert.Equal(fieldId, Assert.Single(table.CustomFieldColumns).FieldDefinitionId);
        _templateServiceMock.Verify(s => s.SaveTemplateAsync(It.IsAny<SeriesResultsTemplate>()), Times.Never);
    }

    [Fact]
    public async Task Preview_NoSeriesChosen_UsesMostRecentSeries()
    {
        var series = SetupSeries(_clubId);
        _templateServiceMock.Setup(s => s.GetPreviewSeriesAsync(_clubId, 1))
            .ReturnsAsync(new List<Series> { new() { Id = series.Id } });

        var result = await _controller.Preview(ClubInitials, null, new SeriesResultsTemplateViewModel());

        var table = Assert.IsType<ResultsTableViewModel>(Assert.IsType<PartialViewResult>(result).Model);
        Assert.Equal(series.Id, table.SeriesId);
    }

    [Fact]
    public async Task Preview_MissingName_StillShowsTable()
    {
        var series = SetupSeries(_clubId);
        _controller.ModelState.AddModelError(nameof(SeriesResultsTemplateViewModel.Name), "Required");

        var result = await _controller.Preview(ClubInitials, series.Id, new SeriesResultsTemplateViewModel());

        Assert.IsType<ResultsTableViewModel>(Assert.IsType<PartialViewResult>(result).Model);
    }

    [Fact]
    public async Task Preview_InvalidModel_ShowsNoTable()
    {
        var series = SetupSeries(_clubId);
        _controller.ModelState.AddModelError(nameof(SeriesResultsTemplateViewModel.TotalHeader), "Too long");

        var result = await _controller.Preview(ClubInitials, series.Id, new SeriesResultsTemplateViewModel());

        Assert.Null(Assert.IsType<PartialViewResult>(result).Model);
    }

    [Fact]
    public async Task Preview_AnotherClubsSeries_ShowsNoTable()
    {
        var series = SetupSeries(Guid.NewGuid());

        var result = await _controller.Preview(ClubInitials, series.Id, new SeriesResultsTemplateViewModel());

        Assert.Null(Assert.IsType<PartialViewResult>(result).Model);
    }

    [Fact]
    public async Task Edit_Get_LoadsPreviewSeries()
    {
        var template = SetupTemplate(_clubId);
        var recent = new List<Series> { new() { Id = Guid.NewGuid(), Name = "Recent" } };
        _templateServiceMock.Setup(s => s.GetPreviewSeriesAsync(_clubId, It.IsAny<int>()))
            .ReturnsAsync(recent);

        var result = await _controller.Edit(ClubInitials, template.Id);

        var vm = Assert.IsType<SeriesResultsTemplateViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Same(recent, vm.PreviewSeries);
    }

    [Fact]
    public async Task SetDateFormat_ValidFormat_SavesAndClearsClubCache()
    {
        var result = await _controller.SetDateFormat(ClubInitials, "d MMM");

        _templateServiceMock.Verify(s => s.SetClubDateFormatAsync(_clubId, "d MMM"), Times.Once);
        _webClubServiceMock.Verify(s => s.InvalidateClubCache(ClubInitials), Times.Once);
        Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
    }

    [Fact]
    public async Task SetDateFormat_ServiceRejectsFormat_ShowsErrorAndKeepsCache()
    {
        _templateServiceMock.Setup(s => s.SetClubDateFormatAsync(_clubId, "bad"))
            .ThrowsAsync(new InvalidOperationException("Unsupported"));

        await _controller.SetDateFormat(ClubInitials, "bad");

        Assert.Equal("Unsupported", _controller.TempData["ErrorMessage"]);
        _webClubServiceMock.Verify(s => s.InvalidateClubCache(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Edit_Get_ShowsCustomFieldsOnlyWhenClubEnablesThem(bool enabled)
    {
        var template = SetupTemplate(_clubId);
        _clubServiceMock.Setup(s => s.GetMinimalClub(_clubId))
            .ReturnsAsync(new Club { Id = _clubId, EnableCustomCompetitorFields = enabled });

        var result = await _controller.Edit(ClubInitials, template.Id);

        var vm = Assert.IsType<SeriesResultsTemplateViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(enabled, vm.CustomFieldsEnabled);
    }
}
