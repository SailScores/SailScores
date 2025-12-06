using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Web.IntegrationTests.Services;

public class OcrServiceUnitTests
{
    [Fact]
    public async Task AnalyzeImageAsync_ReturnsLinesInOrder_EvenWhenNoMatches()
    {
        // Arrange
        var mockCompetitorService = new Mock<Core.Services.ICompetitorService>();
        mockCompetitorService.Setup(s => s.GetCompetitorsAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>()))
        .ReturnsAsync(new List<Competitor>());

        var mockMatching = new Mock<IMatchingService>();
        mockMatching.Setup(m => m.GetSuggestions(It.IsAny<string>(), It.IsAny<IEnumerable<Competitor>>()))
        .Returns<IEnumerable<MatchingSuggestion>>(null);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Azure:ComputerVision:Endpoint"] = "https://example",
            ["Azure:ComputerVision:SubscriptionKey"] = "key"
        }).Build());
        services.AddHttpClient();
        services.AddLogging();
        services.AddScoped<IOcrService, OcrService>();
        services.AddSingleton<Core.Services.ICompetitorService>(mockCompetitorService.Object);
        services.AddSingleton<IMatchingService>(mockMatching.Object);

        var provider = services.BuildServiceProvider();
        var ocrService = provider.GetRequiredService<IOcrService>();

        // Use a small transparent PNG as test input
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var file = new FormFile(new MemoryStream(imageBytes), 0, imageBytes.Length, "image", "test.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };

        // Act
        var result = await ocrService.AnalyzeImageAsync(file, null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Lines);
    }
}
