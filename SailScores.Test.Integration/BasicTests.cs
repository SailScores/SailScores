using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Integration
{
    public class BasicTests
        : IClassFixture<WebApplicationFactory<SailScores.Web.Startup>>
    {
        private readonly WebApplicationFactory<SailScores.Web.Startup> _factory;

        public BasicTests(WebApplicationFactory<SailScores.Web.Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/Home/About")]
        [InlineData("/Home/News")]
        public async Task MvcEndpoints_ReturnSuccessAndCorrectContentType(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url, GetTestCancellationToken());

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("text/html; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
        }

        [Theory]
        [InlineData("/swagger")]
        public async Task SwaggerEndpointsReturnSuccessAndCorrectContentType(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url, GetTestCancellationToken());

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Contains("text/html",
                response.Content.Headers.ContentType.ToString());
        }

        [Theory]
        [InlineData("/api/club")]
        public async Task ApiEndpointsReturnSuccessAndCorrectContentType(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url, GetTestCancellationToken());

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Contains("application/json",
                response.Content.Headers.ContentType.ToString());
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
