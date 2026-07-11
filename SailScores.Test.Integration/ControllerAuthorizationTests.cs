using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SailScores.Test.Integration
{
    public class ControllerAuthorizationTests
        : IClassFixture<WebApplicationFactory<SailScores.Web.Startup>>
    {
        private readonly WebApplicationFactory<SailScores.Web.Startup> _factory;

        public ControllerAuthorizationTests(WebApplicationFactory<SailScores.Web.Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/BoatClass/Create?clubInitials=LHYC")]
        [InlineData("/Competitor/Create?clubInitials=LHYC")]
        public async Task ProtectedEndpoints_RedirectToLogin_WhenAnonymous(string url)
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Act
            var response = await client.GetAsync(url, GetTestCancellationToken());

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location.ToString());
        }

        [Theory]
        [InlineData("/api/races")]
        public async Task ApiEndpoints_Return401_WhenAnonymous(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url, GetTestCancellationToken());

            // Assert
            // Note: Api endpoints are configured in Startup.cs to return 401 instead of redirecting
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static CancellationToken GetTestCancellationToken()
        {
            var testContextType = Type.GetType("Xunit.TestContext, xunit.v3", throwOnError: false);
            var currentProperty = testContextType?.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
            var currentContext = currentProperty?.GetValue(null);
            var cancellationTokenProperty = currentContext?.GetType().GetProperty("CancellationToken");
            return cancellationTokenProperty?.GetValue(currentContext) is CancellationToken token
                ? token
                : CancellationToken.None;
        }
    }
}
