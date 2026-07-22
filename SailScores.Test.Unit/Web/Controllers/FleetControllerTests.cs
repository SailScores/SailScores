using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SailScores.Core.Mapping;
using SailScores.Web.Controllers;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Web.Controllers;

public class FleetControllerTests
{
    private readonly FleetController _controller;
    private readonly Mock<SailScores.Core.Services.IClubService> _clubServiceMock;
    private readonly Mock<IFleetService> _fleetServiceMock;
    private readonly Mock<IAuthorizationService> _authServiceMock;
    private readonly IMapper _mapper;

    private const string ClubInitials = "LHYC";

    public FleetControllerTests()
    {
        var config = new MapperConfiguration(opts =>
        {
            opts.AddProfile(new DbToModelMappingProfile());
        });
        _mapper = config.CreateMapper();

        _clubServiceMock = ControllerTestUtilities.MakeCoreClubServiceMock();
        _fleetServiceMock = new Mock<IFleetService>();
        _authServiceMock = ControllerTestUtilities.MakeAuthServiceMock();

        _controller = new FleetController(
            _clubServiceMock.Object,
            _fleetServiceMock.Object,
            _authServiceMock.Object,
            _mapper);
    }

    [Fact]
    public async Task Manage_Always_ReturnsViewWithFleetManagementViewModel()
    {
        var vm = new FleetManagementViewModel();
        _fleetServiceMock
            .Setup(s => s.GetFleetManagementViewModel(ClubInitials))
            .ReturnsAsync(vm);

        var result = await _controller.Manage(ClubInitials);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<FleetManagementViewModel>(viewResult.Model);
        Assert.Equal(ClubInitials, model.ClubInitials);
        _fleetServiceMock.Verify(s => s.GetFleetManagementViewModel(ClubInitials), Times.Once);
    }
}
