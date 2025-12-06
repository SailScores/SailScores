using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Services.Interfaces;
using SailScores.Web.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services;

public class OcrServiceTests
{
    private class TestHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public TestHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_response);
    }

    [Fact]
    public async Task AnalyzeImageAsync_Returns_AllLines_InOrder()
    {
        // Arrange
        var mockCompetitorService = new Mock<SailScores.Core.Services.ICompetitorService>();
        mockCompetitorService.Setup(s => s.GetCompetitorsAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>()))
        .ReturnsAsync(new List<Competitor>());

        var mockMatching = new Mock<IMatchingService>();
        // return no suggestions so we can verify lines preserved
        mockMatching.Setup(m => m.GetSuggestions(It.IsAny<string>(), It.IsAny<IEnumerable<Competitor>>()))
        .Returns((IEnumerable<MatchingSuggestion>?)null);

        // Create fake Azure response JSON with two lines in order
        var json = "{\"readResult\":{\"blocks\":[{\"lines\":[{\"content\":\"LINE ONE\"},{\"content\":\"LINE TWO\"}]}]}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var handler = new TestHandler(response);
        var httpClient = new HttpClient(handler);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Azure:ComputerVision:Endpoint"] = "https://example",
            ["Azure:ComputerVision:SubscriptionKey"] = "key"
        }).Build());
        services.AddSingleton<IHttpClientFactory>(mockFactory.Object);
        services.AddLogging();
        services.AddScoped<IOcrService, OcrService>();
        services.AddSingleton<SailScores.Core.Services.ICompetitorService>(mockCompetitorService.Object);
        services.AddSingleton<IMatchingService>(mockMatching.Object);

        var provider = services.BuildServiceProvider();
        var ocrService = provider.GetRequiredService<IOcrService>();

        // Minimal PNG-like bytes
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var file = new FormFile(new MemoryStream(imageBytes), 0, imageBytes.Length, "image", "test.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };

        // Act
        var result = await ocrService.AnalyzeImageAsync(file, null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Lines);
        var lines = result.Lines.Select(l => l.Text).ToList();
        Assert.Equal(new[] { "LINE ONE", "LINE TWO" }, lines);
    }

    [Fact]
    public async Task AnalyzeImageAsync_IncludesSuggestions_WhenMatchingServiceProvides()
    {
        // Arrange
        var mockCompetitorService = new Mock<SailScores.Core.Services.ICompetitorService>();
        mockCompetitorService.Setup(s => s.GetCompetitorsAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>()))
        .ReturnsAsync(new List<Competitor>());

        var comp = new Competitor { Id = Guid.NewGuid(), SailNumber = "123", Name = "Boat123" };
        var suggestion = new MatchingSuggestion(comp, 0.9, "123", true);

        var mockMatching = new Mock<IMatchingService>();
        mockMatching.Setup(m => m.GetSuggestions("LINE ONE", It.IsAny<IEnumerable<Competitor>>()))
        .Returns(new[] { suggestion });
        mockMatching.Setup(m => m.GetSuggestions("LINE TWO", It.IsAny<IEnumerable<Competitor>>()))
        .Returns((IEnumerable<MatchingSuggestion>?)null);

        // Fake Azure response
        var json = "{\"readResult\":{\"blocks\":[{\"lines\":[{\"content\":\"LINE ONE\"},{\"content\":\"LINE TWO\"}]}]}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var handler = new TestHandler(response);
        var httpClient = new HttpClient(handler);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Azure:ComputerVision:Endpoint"] = "https://example",
            ["Azure:ComputerVision:SubscriptionKey"] = "key"
        }).Build());
        services.AddSingleton<IHttpClientFactory>(mockFactory.Object);
        services.AddLogging();
        services.AddScoped<IOcrService, OcrService>();
        services.AddSingleton<SailScores.Core.Services.ICompetitorService>(mockCompetitorService.Object);
        services.AddSingleton<IMatchingService>(mockMatching.Object);

        var provider = services.BuildServiceProvider();
        var ocrService = provider.GetRequiredService<IOcrService>();

        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var file = new FormFile(new MemoryStream(imageBytes), 0, imageBytes.Length, "image", "test.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };

        // Act
        var result = await ocrService.AnalyzeImageAsync(file, null, null, null);

        // Assert
        Assert.NotNull(result);
        var lines = result.Lines.ToList();
        Assert.Equal(2, lines.Count);
        Assert.Equal("LINE ONE", lines[0].Text);
        Assert.Single(lines[0].Suggestions);
        Assert.Equal("LINE TWO", lines[1].Text);
        Assert.Empty(lines[1].Suggestions);
    }
}
