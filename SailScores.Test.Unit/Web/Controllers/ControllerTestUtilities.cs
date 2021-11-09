using Moq;
using SailScores.Web.Services;
using System;
using System.Collections.Generic;
using SailScores.Core.Model;
using SailScores.Web.Services.Interfaces;

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

            coreClubService.Setup(c => c.GetAllFleets(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Fleet>());
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
    }
}
