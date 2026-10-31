using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SailScores.Identity.Entities;
using SailScores.Web.Controllers;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Web.Controllers;

public class RaceControllerTests
{
    private readonly Mock<IRaceService> _raceServiceMock = new();
    private readonly RaceController _controller;

    public RaceControllerTests()
    {
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null, null, null, null, null, null, null, null);

        _controller = new RaceController(
            ControllerTestUtilities.MakeCoreClubServiceMock().Object,
            _raceServiceMock.Object,
            Mock.Of<IAuthorizationService>(),
            Mock.Of<IAdminTipService>(),
            userManagerMock.Object,
            Mock.Of<ISpeechService>(),
            Mock.Of<IMapper>());
    }

    [Fact]
    public async Task Index_FiltersNotValidForSeason_ReturnsNotFound()
    {
        _raceServiceMock
            .Setup(r => r.GetAllRaceSummariesAsync("LHYC", "2026", "1999-01", null, true, true))
            .ReturnsAsync((RaceSummaryListViewModel)null);

        var result = await _controller.Index("lhyc", "2026", month: "1999-01");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_WithFilters_PassesThemToService()
    {
        var races = new RaceSummaryListViewModel();
        _raceServiceMock
            .Setup(r => r.GetAllRaceSummariesAsync("LHYC", "2026", "2026-07", "lasers", true, true))
            .ReturnsAsync(races);

        var result = await _controller.Index("lhyc", "2026", "2026-07", "lasers");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ClubItemViewModel<RaceSummaryListViewModel>>(view.Model);
        Assert.Same(races, model.Item);
    }
}
