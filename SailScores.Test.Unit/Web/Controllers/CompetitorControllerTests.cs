using Microsoft.AspNetCore.Mvc;
using Moq;
using SailScores.Web.Controllers;
using SailScores.Web.Services;
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
            _userManagerMock = new Mock<UserManager<ApplicationUser>>();


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

            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task Create_Post_CallsServiceSaveNew()
        {
            SetupAsAuthorized();


            var vm = new CompetitorWithOptionsViewModel();
            await _controller.Create(_clubInitials, vm);

            _competitorServiceMock.Verify(s => s.SaveAsync(vm), Times.Once);

        }

        [Fact]
        public async Task Create_PostUnauthorized_ReturnForbidden()
        {

            var vm = new CompetitorWithOptionsViewModel();
            var result = await _controller.Create(_clubInitials, vm);

            _competitorServiceMock.Verify(s => s.SaveAsync(vm), Times.Never);
            Assert.IsType<UnauthorizedResult>(result);
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
            _competitorServiceMock.Verify(s => s.SaveAsync(vm), Times.Never);
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
                It.IsAny<CompetitorWithOptionsViewModel>())).Throws(new InvalidOperationException());

            // Act
            var result = await _controller.Create(_clubInitials, vm);

            // Assert
            var viewresult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewresult.Model);
            Assert.Equal(vm, viewresult.Model);
        }


        private void SetupAsAuthorized()
        {
            _authServiceMock.Setup(s =>
                    s.CanUserEdit(It.IsAny<ClaimsPrincipal>(), It.IsAny<Guid>()))
                .ReturnsAsync(true);
        }
    }
}
