using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using SailScores.Web.Services;

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
            return new Mock<SailScores.Core.Services.IClubService>();
        }

        internal static Mock<IAuthorizationService> MakeAuthServiceMock()
        {
            return new Mock<IAuthorizationService>();
        }

    }
}
