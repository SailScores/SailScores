using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using SailScores.Identity.Entities;
using SailScores.Web.Controllers;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using Xunit;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Test.Unit.Web.Controllers;

public class ClubRequestControllerTests
{
    [Fact]
    public async Task RequestAccountAndClub_TurnstileInvalid_ReturnsViewAndDoesNotCreateUser()
    {
        var clubRequestServiceMock = new Mock<IClubRequestService>();
        var turnstileServiceMock = new Mock<ITurnstileService>();
        turnstileServiceMock
            .Setup(s => s.VerifyAsync(
                "bad-token",
                It.IsAny<IPAddress>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);

        var controller = new ClubRequestController(
            clubRequestServiceMock.Object,
            new Mock<IAuthorizationService>().Object,
            userManagerMock.Object,
            signInManagerMock.Object,
            Mock.Of<ILogger<AccountController>>(),
            turnstileServiceMock.Object);

        SetHttpContext(controller, "bad-token");
        var model = CreateRequestModel();

        var result = await controller.RequestAccountAndClub(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
        Assert.Contains(
            controller.ModelState,
            entry => entry.Value.Errors.Any(
                e => e.ErrorMessage == "Please complete the captcha challenge."));
        userManagerMock.Verify(
            m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
        clubRequestServiceMock.Verify(
            m => m.SubmitRequest(It.IsAny<ClubRequestViewModel>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestAccountAndClub_TurnstileValid_SubmitsRequest()
    {
        var clubRequestServiceMock = new Mock<IClubRequestService>();
        clubRequestServiceMock.Setup(m => m.AreInitialsAllowed("SSC")).ReturnsAsync(true);
        clubRequestServiceMock
            .Setup(m => m.SubmitRequest(It.IsAny<ClubRequestViewModel>()))
            .Returns(Task.CompletedTask);

        var turnstileServiceMock = new Mock<ITurnstileService>();
        turnstileServiceMock
            .Setup(s => s.VerifyAsync(
                "good-token",
                It.IsAny<IPAddress>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var userManagerMock = CreateUserManagerMock();
        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password1!"))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock
            .Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("confirmation-token");

        var signInManagerMock = CreateSignInManagerMock(userManagerMock);
        signInManagerMock
            .Setup(m => m.SignInAsync(It.IsAny<ApplicationUser>(), false, null))
            .Returns(Task.CompletedTask);

        var controller = new ClubRequestController(
            clubRequestServiceMock.Object,
            new Mock<IAuthorizationService>().Object,
            userManagerMock.Object,
            signInManagerMock.Object,
            Mock.Of<ILogger<AccountController>>(),
            turnstileServiceMock.Object);

        SetHttpContext(controller, "good-token");
        var model = CreateRequestModel();

        var result = await controller.RequestAccountAndClub(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("RequestSubmitted", viewResult.ViewName);
        clubRequestServiceMock.Verify(
            m => m.SubmitRequest(It.IsAny<ClubRequestViewModel>()),
            Times.Once);
        userManagerMock.Verify(
            m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password1!"),
            Times.Once);
    }

    private static AccountAndClubRequestViewModel CreateRequestModel()
    {
        return new AccountAndClubRequestViewModel
        {
            ClubName = "SailScores Club",
            ClubInitials = "SSC",
            ContactEmail = "newuser@example.com",
            ContactFirstName = "New",
            ContactLastName = "User",
            Password = "Password1!",
            ConfirmPassword = "Password1!"
        };
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock(
        Mock<UserManager<ApplicationUser>> userManagerMock)
    {
        return new Mock<SignInManager<ApplicationUser>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<ApplicationUser>>());
    }

    private static void SetHttpContext(Controller controller, string turnstileToken)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        httpContext.Request.Form = new FormCollection(
            new Dictionary<string, StringValues>
            {
                ["cf-turnstile-response"] = turnstileToken
            });

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }
}
