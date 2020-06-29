using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SailScores.Core.Services;
using SailScores.Web.Controllers;
using SailScores.Web.Services;
using Xunit;
using ClubService = SailScores.Web.Services.ClubService;

namespace SailScores.Test.Unit.Web.Controllers
{
    public class BoatClassControllerTests
    {
        private Mock<SailScores.Core.Services.IClubService> _clubServiceMock;
        private Mock<IAuthorizationService> _authServiceMock;
        private Mock<IBoatClassService> _classServiceMock;
        private BoatClassController _controller;

        public BoatClassControllerTests()
        {
            _clubServiceMock = ControllerTestUtilities.MakeCoreClubServiceMock();
            _authServiceMock = ControllerTestUtilities.MakeAuthServiceMock();

            _classServiceMock = new Mock<IBoatClassService>();

            _controller = new BoatClassController(
                _clubServiceMock.Object,
                _classServiceMock.Object,
                _authServiceMock.Object);
        }

        [Fact]
        public async Task a()
        {
            SetupAsAuthorized();

            var vm = _controller.Create();

        }

        private void SetupAsAuthorized()
        {
            _authServiceMock.Setup(s =>
                    s.CanUserEdit(It.IsAny<ClaimsPrincipal>(), It.IsAny<Guid>()))
                .ReturnsAsync(true);
        }
    }
}
