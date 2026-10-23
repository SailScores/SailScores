using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
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
    private readonly SeriesResultsTemplateController _controller;

    public SeriesResultsTemplateControllerTests()
    {
        _clubServiceMock.Setup(s => s.GetClubId(ClubInitials)).ReturnsAsync(_clubId);
        _controller = new SeriesResultsTemplateController(
            _templateServiceMock.Object,
            _clubServiceMock.Object,
            _competitorFieldServiceMock.Object)
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
            DefaultRegattaSeriesResultsTemplateId = Guid.NewGuid()
        };

        var result = await _controller.SetDefaults(ClubInitials, model);

        _templateServiceMock.Verify(s => s.SetClubDefaultsAsync(
            _clubId, model.DefaultSeriesResultsTemplateId, model.DefaultRegattaSeriesResultsTemplateId),
            Times.Once);
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
}
