using Microsoft.AspNetCore.Mvc;
using Moq;
using SailScores.Web.Controllers;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using SailScores.Core.Mapping;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using Xunit;
using ICompetitorService = SailScores.Web.Services.Interfaces.ICompetitorService;
using SailScores.Core.Services;
using Microsoft.AspNetCore.Identity;
using SailScores.Identity.Entities;
using SailScores.Core.Model;

namespace SailScores.Test.Unit.Web.Controllers
{
    public class CompetitorControllerTests
    {
        private readonly CompetitorController _controller;

        private readonly IMapper _mapper;
        private readonly Mock<SailScores.Core.Services.IClubService> _clubServiceMock;
        private readonly Mock<IAuthorizationService> _authServiceMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<ICsvService> _csvServiceMock;
        private readonly Mock<ICompetitorService> _competitorServiceMock;
        private readonly Mock<IForwarderService> _forwarderServiceMock;
        private readonly Mock<IAdminTipService> _adminTipServiceMock;


        private readonly string _clubInitials = "LHYC";

        public CompetitorControllerTests()
        {
            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new DbToModelMappingProfile());
            });

            _mapper = config.CreateMapper();

            _clubServiceMock = ControllerTestUtilities.MakeCoreClubServiceMock();
            _competitorServiceMock = ControllerTestUtilities.MakeWebCompetitorServiceMock();
            _forwarderServiceMock = ControllerTestUtilities.MakeForwarderServiceMock();
            _adminTipServiceMock = ControllerTestUtilities.MakeAdminTipServiceMock();
            _authServiceMock = ControllerTestUtilities.MakeAuthServiceMock();
            _csvServiceMock = new Mock<ICsvService>();
            _authServiceMock = ControllerTestUtilities.MakeAuthServiceMock();
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);
            _userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { FirstName = "Test", LastName = "User" });

            _controller = new CompetitorController(
                _clubServiceMock.Object,
                _competitorServiceMock.Object,
                _forwarderServiceMock.Object,
                _authServiceMock.Object,
                _csvServiceMock.Object,
                _adminTipServiceMock.Object,
                _userManagerMock.Object,
                _mapper);
        }

        [Fact]
        public async Task Create_DoesNotThrow()
        {
            SetupAsAuthorized();

            var action = await _controller.Create(_clubInitials);

            Assert.IsType<ViewResult>(action);
        }

        [Fact]
        public async Task Create_Post_Redirects()
        {
            SetupAsAuthorized();

            var vm = new CompetitorWithOptionsViewModel();
            var result = await _controller.Create(_clubInitials, vm);

            //Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Competitor", redirect.ControllerName);
        }

        [Fact]
        public async Task Create_Post_CallsServiceSaveNew()
        {
            SetupAsAuthorized();

            var vm = new CompetitorWithOptionsViewModel();
            await _controller.Create(_clubInitials, vm);

            _competitorServiceMock.Verify(s => s.SaveAsync(vm, "Test User"), Times.Once);
        }

        [Fact]
        public async Task Create_PostInvalidModel_ReturnsModel()
        {
            SetupAsAuthorized();

            var vm = new CompetitorWithOptionsViewModel();
            _controller.ModelState.AddModelError("Name", "The Name field is required.");

            // Act
            var result = await _controller.Create(_clubInitials, vm);

            // Assert
            _competitorServiceMock.Verify(s => s.SaveAsync(vm, string.Empty), Times.Never);
            var viewresult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewresult.Model);
            Assert.Equal(vm, viewresult.Model);
        }

        [Fact]
        public async Task CreatePost_ServiceThrows_ReturnsModel()
        {
            // Arrange
            SetupAsAuthorized();

            var vm = new CompetitorWithOptionsViewModel();
            _competitorServiceMock.Setup(s => s.SaveAsync(
                It.IsAny<CompetitorWithOptionsViewModel>(), It.IsAny<string>())).Throws(new InvalidOperationException());

            // Act
            var result = await _controller.Create(_clubInitials, vm);

            // Assert
            var viewresult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewresult.Model);
            Assert.Equal(vm, viewresult.Model);
        }

        [Fact]
        public async Task PostSetAlternativeSailNumber_InvalidModel_ReturnsBadRequest()
        {
            var model = new CompetitorAlternativeSailNumberUpdateViewModel
            {
                CompetitorId = Guid.NewGuid(),
                AlternativeSailNumber = "ALT-1"
            };
            _controller.ModelState.AddModelError("AlternativeSailNumber", "invalid");

            var result = await _controller.PostSetAlternativeSailNumber(_clubInitials, model);

            Assert.IsType<BadRequestObjectResult>(result);
            _competitorServiceMock.Verify(s => s.SetAlternativeSailNumber(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task PostSetAlternativeSailNumber_CompetitorMissing_ReturnsNotFound()
        {
            var model = new CompetitorAlternativeSailNumberUpdateViewModel
            {
                CompetitorId = Guid.NewGuid(),
                AlternativeSailNumber = "ALT-1"
            };

            _competitorServiceMock
                .Setup(s => s.GetCompetitorAsync(model.CompetitorId))
                .ReturnsAsync((Competitor)null);

            var result = await _controller.PostSetAlternativeSailNumber(_clubInitials, model);

            Assert.IsType<NotFoundResult>(result);
            _competitorServiceMock.Verify(s => s.SetAlternativeSailNumber(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task PostSetAlternativeSailNumber_Unauthorized_ReturnsUnauthorized()
        {
            var competitorId = Guid.NewGuid();
            var clubId = Guid.NewGuid();
            var model = new CompetitorAlternativeSailNumberUpdateViewModel
            {
                CompetitorId = competitorId,
                AlternativeSailNumber = "ALT-1"
            };

            _competitorServiceMock
                .Setup(s => s.GetCompetitorAsync(competitorId))
                .ReturnsAsync(new Competitor { Id = competitorId, ClubId = clubId });

            SetupAltSailPermissions(clubId, canEdit: false);

            var result = await _controller.PostSetAlternativeSailNumber(_clubInitials, model);

            Assert.IsType<UnauthorizedResult>(result);
            _competitorServiceMock.Verify(s => s.SetAlternativeSailNumber(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task PostSetAlternativeSailNumber_Authorized_ReturnsJsonAndCallsService()
        {
            var competitorId = Guid.NewGuid();
            var clubId = Guid.NewGuid();
            var model = new CompetitorAlternativeSailNumberUpdateViewModel
            {
                CompetitorId = competitorId,
                AlternativeSailNumber = " ALT-77 "
            };

            _competitorServiceMock
                .Setup(s => s.GetCompetitorAsync(competitorId))
                .ReturnsAsync(new Competitor { Id = competitorId, ClubId = clubId });

            SetupAltSailPermissions(clubId, canEdit: true);

            var result = await _controller.PostSetAlternativeSailNumber(_clubInitials, model);

            var json = Assert.IsType<JsonResult>(result);
            Assert.NotNull(json.Value);

            var payloadType = json.Value.GetType();
            var responseCompetitorId = (Guid)payloadType.GetProperty("competitorId")!.GetValue(json.Value)!;
            var responseAltSailNumber = (string)payloadType.GetProperty("alternativeSailNumber")!.GetValue(json.Value)!;

            Assert.Equal(competitorId, responseCompetitorId);
            Assert.Equal("ALT-77", responseAltSailNumber);

            _competitorServiceMock.Verify(s => s.SetAlternativeSailNumber(competitorId, " ALT-77 ", "Test User"), Times.Once);
        }

        private void SetupAltSailPermissions(Guid clubId, bool canEdit)
        {
            _authServiceMock.Setup(s => s.CanUserEditRaces(It.IsAny<ClaimsPrincipal>(), clubId))
                .ReturnsAsync(canEdit);
            _authServiceMock.Setup(s => s.CanUserEditSeries(It.IsAny<ClaimsPrincipal>(), clubId))
                .ReturnsAsync(canEdit);
            _authServiceMock.Setup(s => s.IsUserClubAdministrator(It.IsAny<ClaimsPrincipal>(), clubId))
                .ReturnsAsync(canEdit);
            _authServiceMock.Setup(s => s.IsUserFullAdmin(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(canEdit);
        }

        private void SetupAsAuthorized()
        {
            _authServiceMock.Setup(s =>
                    s.CanUserEdit(It.IsAny<ClaimsPrincipal>(), It.IsAny<Guid>()))
                .ReturnsAsync(true);
        }
    }
}
