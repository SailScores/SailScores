using Moq;
using SailScores.Web.Services;
using System;
using System.Collections.Generic;
using SailScores.Core.Model;
using SailScores.Web.Services.Interfaces;
using IForwarderService = SailScores.Core.Services.IForwarderService;

namespace SailScores.Test.Unit.Web.Controllers
{
    public static class ControllerTestUtilities
    {
        public static Mock<ClubService> MakeClubServiceMock()
        {
            return new Mock<ClubService>();
        }

        public static Mock<SailScores.Core.Services.IClubService> MakeCoreClubServiceMock()
        {
            var coreClubService = new Mock<SailScores.Core.Services.IClubService>();

            var testClubId = Guid.NewGuid();
            coreClubService.Setup(c => c.GetAllFleets(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Fleet>());
            coreClubService.Setup(c => c.GetClubId(It.IsAny<string>()))
                .ReturnsAsync(testClubId);
            coreClubService.Setup(c => c.GetMinimalClub(It.IsAny<Guid>()))
                .ReturnsAsync(new Club { Id = testClubId, EnableCustomCompetitorFields = false });
            coreClubService.Setup(c => c.GetAllBoatClasses(It.IsAny<Guid>()))
                .ReturnsAsync(new List<BoatClass>());
            return coreClubService;
        }

        internal static Mock<IAuthorizationService> MakeAuthServiceMock()
        {
            return new Mock<IAuthorizationService>();
        }

        internal static Mock<ICompetitorService> MakeWebCompetitorServiceMock()
        {
            return new Mock<ICompetitorService>();
        }

        internal static Mock<IAdminTipService> MakeAdminTipServiceMock()
        {
            return new Mock<IAdminTipService>();
        }
        internal static Mock<IForwarderService> MakeForwarderServiceMock()
        {
            return new Mock<IForwarderService>();
        }
    }
}
