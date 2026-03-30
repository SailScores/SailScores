using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using SailScores.Identity.Entities;
using SailScores.Web.Controllers;
using SailScores.Web.Models.AccountViewModels;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using Xunit;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Test.Unit.Web.Controllers;

public class AccountControllerTests
{
    [Fact]
    public async Task Register_TurnstileInvalid_ReturnsViewAndDoesNotCreateUser()
    {
        var userManagerMock = CreateUserManagerMock();
        var signInManagerMock = CreateSignInManagerMock(userManagerMock);
        var emailSenderMock = new Mock<IEmailSender>();
        var authServiceMock = new Mock<IAuthorizationService>();
        var turnstileServiceMock = new Mock<ITurnstileService>();
        turnstileServiceMock
            .Setup(s => s.VerifyAsync(
                "bad-token",
                It.IsAny<IPAddress>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var configuration = new ConfigurationBuilder().Build();
        var controller = new AccountController(
            userManagerMock.Object,
            signInManagerMock.Object,
            emailSenderMock.Object,
            authServiceMock.Object,
            Mock.Of<ILogger<AccountController>>(),
            turnstileServiceMock.Object,
            new AppSettingsService(configuration));

        SetHttpContext(controller, "bad-token");

        var model = new RegisterViewModel
        {
            Email = "newuser@example.com",
            FirstName = "New",
            LastName = "User",
            Password = "Password1!",
            ConfirmPassword = "Password1!"
        };

        var result = await controller.Register(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
        Assert.Contains(
            controller.ModelState,
            entry => entry.Value.Errors.Any(
                e => e.ErrorMessage == "Please complete the captcha challenge."));
        userManagerMock.Verify(
            m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
        emailSenderMock.Verify(
            m => m.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Register_TurnstileValid_CallsCreateUser()
    {
        var userManagerMock = CreateUserManagerMock();
        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password1!"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "creation failed" }));

        var signInManagerMock = CreateSignInManagerMock(userManagerMock);
        var turnstileServiceMock = new Mock<ITurnstileService>();
        turnstileServiceMock
            .Setup(s => s.VerifyAsync(
                "good-token",
                It.IsAny<IPAddress>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var configuration = new ConfigurationBuilder().Build();
        var controller = new AccountController(
            userManagerMock.Object,
            signInManagerMock.Object,
            new Mock<IEmailSender>().Object,
            new Mock<IAuthorizationService>().Object,
            Mock.Of<ILogger<AccountController>>(),
            turnstileServiceMock.Object,
            new AppSettingsService(configuration));

        SetHttpContext(controller, "good-token");

        var model = new RegisterViewModel
        {
            Email = "newuser@example.com",
            FirstName = "New",
            LastName = "User",
            Password = "Password1!",
            ConfirmPassword = "Password1!"
        };

        var result = await controller.Register(model);

        Assert.IsType<ViewResult>(result);
        userManagerMock.Verify(
            m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password1!"),
            Times.Once);
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
