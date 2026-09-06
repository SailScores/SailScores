using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SailScores.Core.Mapping;
using SailScores.Core.Model;
using SailScores.Web.Areas.Api.Controllers;
using SailScores.Web.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Web.Areas.Api.Controllers;

public class FleetsControllerTests
{
    private readonly Mock<SailScores.Core.Services.IClubService> _clubServiceMock;
    private readonly Mock<SailScores.Core.Services.IFleetService> _coreFleetServiceMock;
    private readonly Mock<SailScores.Core.Services.ICompetitorService> _coreCompetitorServiceMock;
    private readonly Mock<IAuthorizationService> _authServiceMock;
    private readonly IMapper _mapper;
    private readonly FleetsController _controller;

    private readonly Guid _clubId = Guid.NewGuid();
    private readonly Guid _fleetId = Guid.NewGuid();
    private readonly Guid _competitorId = Guid.NewGuid();

    public FleetsControllerTests()
    {
        var config = new MapperConfiguration(opts =>
        {
            opts.AddProfile(new DbToModelMappingProfile());
        });
        _mapper = config.CreateMapper();

        _clubServiceMock = new Mock<SailScores.Core.Services.IClubService>();
        _coreFleetServiceMock = new Mock<SailScores.Core.Services.IFleetService>();
        _coreCompetitorServiceMock = new Mock<SailScores.Core.Services.ICompetitorService>();
        _authServiceMock = new Mock<IAuthorizationService>();

        _controller = new FleetsController(
            _clubServiceMock.Object,
            _coreFleetServiceMock.Object,
            _coreCompetitorServiceMock.Object,
            _authServiceMock.Object,
            _mapper)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            }
        };
    }

    private Fleet MakeFleet(SailScores.Api.Enumerations.FleetType fleetType = SailScores.Api.Enumerations.FleetType.SelectedBoats)
        => new() { Id = _fleetId, ClubId = _clubId, FleetType = fleetType };

    private Competitor MakeCompetitor(Guid? clubId = null)
        => new() { Id = _competitorId, ClubId = clubId ?? _clubId };

    [Fact]
    public async Task AddCompetitor_UserIsClubAdmin_ReturnsOkAndCallsAddCompetitorToFleet()
    {
        _coreFleetServiceMock.Setup(s => s.Get(_fleetId)).ReturnsAsync(MakeFleet());
        _coreCompetitorServiceMock.Setup(s => s.GetCompetitorAsync(_competitorId)).ReturnsAsync(MakeCompetitor());
        _authServiceMock.Setup(s => s.IsUserClubAdministrator(It.IsAny<ClaimsPrincipal>(), _clubId)).ReturnsAsync(true);

        var result = await _controller.AddCompetitor(_fleetId, _competitorId);

        Assert.IsType<OkResult>(result);
        _coreFleetServiceMock.Verify(s => s.AddCompetitorToFleet(_fleetId, _competitorId), Times.Once);
    }

    [Fact]
    public async Task AddCompetitor_UserIsNotAdmin_ReturnsUnauthorizedAndDoesNotCallService()
    {
        _coreFleetServiceMock.Setup(s => s.Get(_fleetId)).ReturnsAsync(MakeFleet());
        _authServiceMock.Setup(s => s.IsUserClubAdministrator(It.IsAny<ClaimsPrincipal>(), _clubId)).ReturnsAsync(false);
        _authServiceMock.Setup(s => s.IsUserFullAdmin(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(false);

        var result = await _controller.AddCompetitor(_fleetId, _competitorId);

        Assert.IsType<UnauthorizedResult>(result);
        _coreFleetServiceMock.Verify(s => s.AddCompetitorToFleet(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AddCompetitor_FleetDoesNotExist_ReturnsNotFound()
    {
        _coreFleetServiceMock.Setup(s => s.Get(_fleetId)).ReturnsAsync((Fleet)null);

        var result = await _controller.AddCompetitor(_fleetId, _competitorId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AddCompetitor_FleetNotSelectedBoatsType_ReturnsBadRequest()
    {
        _coreFleetServiceMock.Setup(s => s.Get(_fleetId))
            .ReturnsAsync(MakeFleet(SailScores.Api.Enumerations.FleetType.AllBoatsInClub));
        _authServiceMock.Setup(s => s.IsUserClubAdministrator(It.IsAny<ClaimsPrincipal>(), _clubId)).ReturnsAsync(true);

        var result = await _controller.AddCompetitor(_fleetId, _competitorId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddCompetitor_CompetitorBelongsToDifferentClub_ReturnsNotFound()
    {
        _coreFleetServiceMock.Setup(s => s.Get(_fleetId)).ReturnsAsync(MakeFleet());
        _authServiceMock.Setup(s => s.IsUserClubAdministrator(It.IsAny<ClaimsPrincipal>(), _clubId)).ReturnsAsync(true);
        _coreCompetitorServiceMock.Setup(s => s.GetCompetitorAsync(_competitorId))
            .ReturnsAsync(MakeCompetitor(Guid.NewGuid()));

        var result = await _controller.AddCompetitor(_fleetId, _competitorId);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RemoveCompetitor_UserIsClubAdmin_ReturnsOkAndCallsRemoveCompetitorFromFleet()
    {
        _coreFleetServiceMock.Setup(s => s.Get(_fleetId)).ReturnsAsync(MakeFleet());
        _coreCompetitorServiceMock.Setup(s => s.GetCompetitorAsync(_competitorId)).ReturnsAsync(MakeCompetitor());
        _authServiceMock.Setup(s => s.IsUserClubAdministrator(It.IsAny<ClaimsPrincipal>(), _clubId)).ReturnsAsync(true);

        var result = await _controller.RemoveCompetitor(_fleetId, _competitorId);

        Assert.IsType<OkResult>(result);
        _coreFleetServiceMock.Verify(s => s.RemoveCompetitorFromFleet(_fleetId, _competitorId), Times.Once);
    }

    [Fact]
    public async Task RemoveCompetitor_UserIsNotAdmin_ReturnsUnauthorized()
    {
        _coreFleetServiceMock.Setup(s => s.Get(_fleetId)).ReturnsAsync(MakeFleet());
        _authServiceMock.Setup(s => s.IsUserClubAdministrator(It.IsAny<ClaimsPrincipal>(), _clubId)).ReturnsAsync(false);
        _authServiceMock.Setup(s => s.IsUserFullAdmin(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(false);

        var result = await _controller.RemoveCompetitor(_fleetId, _competitorId);

        Assert.IsType<UnauthorizedResult>(result);
    }
}
