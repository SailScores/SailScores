using Microsoft.AspNetCore.Mvc;
using Moq;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Controllers;
using SailScores.Web.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using SailScores.Web.Services.Interfaces;
using Xunit;

namespace SailScores.Test.Unit.Web.Controllers
{
    public class BoatClassControllerTests
    {
        private readonly Mock<SailScores.Core.Services.IClubService> _clubServiceMock;
        private readonly Mock<IAuthorizationService> _authServiceMock;
        private readonly Mock<SailScores.Web.Services.Interfaces.IBoatClassService> _classServiceMock;
        private readonly BoatClassController _controller;

        private readonly string _clubInitials = "LHYC";

        public BoatClassControllerTests()
        {
            _clubServiceMock = ControllerTestUtilities.MakeCoreClubServiceMock();
            _authServiceMock = ControllerTestUtilities.MakeAuthServiceMock();

            _classServiceMock = new Mock<SailScores.Web.Services.Interfaces.IBoatClassService>();

            _controller = new BoatClassController(
                _clubServiceMock.Object,
                _classServiceMock.Object,
                _authServiceMock.Object);
        }

        [Fact]
        public void Create_DoesNotThrow()
        {
            SetupAsAuthorized();

            var action = _controller.Create();

            Assert.IsType<ViewResult>(action);
        }

        [Fact]
        public async Task Create_Post_Redirects()
        {
            SetupAsAuthorized();

            var vm = new BoatClass
            {

            };
            var result = await _controller.Create(_clubInitials, vm);

            //Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task Create_Post_CallsServiceSaveNew()
        {
            SetupAsAuthorized();

            var vm = new BoatClass
            {

            };
            await _controller.Create(_clubInitials, vm);

            _classServiceMock.Verify(s => s.SaveNew(vm), Times.Once);

        }

        [Fact]
        public async Task Create_PostUnauthorized_ReturnForbidden()
        {
            var vm = new BoatClass
            {

            };
            var result = await _controller.Create(_clubInitials, vm);

            _classServiceMock.Verify(s => s.SaveNew(vm), Times.Never);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Create_PostInvalidModel_ReturnsModel()
        {
            SetupAsAuthorized();
            var vm = new BoatClass
            {
            };
            _controller.ModelState.AddModelError("Name", "The Name field is required.");

            // Act
            var result = await _controller.Create(_clubInitials, vm);

            // Assert
            _classServiceMock.Verify(s => s.SaveNew(vm), Times.Never);
            var viewresult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewresult.Model);
            Assert.Equal(vm, viewresult.Model);
        }

        [Fact]
        public async Task CreatePost_ServiceThrows_ReturnsModel()
        {
            // Arrange
            SetupAsAuthorized();
            var vm = new BoatClass
            {
            };
            _classServiceMock.Setup(s => s.SaveNew(It.IsAny<BoatClass>())).Throws(new InvalidOperationException());

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
