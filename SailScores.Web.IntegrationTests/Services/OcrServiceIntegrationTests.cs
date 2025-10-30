using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces; // Ensure this is present for ICompetitorService
using SailScores.Core.Services; // Add this for IMatchingService
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using SailScores.Core.Model;

namespace SailScores.Web.IntegrationTests.Services;

/// <summary>
/// Integration tests for OcrService that call the actual Azure Computer Vision API.
/// These tests require valid Azure credentials in appsettings.test.json or environment variables.
/// 
/// To run these tests:
/// 1. Set Azure:ComputerVision:Endpoint and Azure:ComputerVision:SubscriptionKey in appsettings.test.json
///    OR set them as environment variables
/// 2. Place test images in TestImages/ directory
/// 3. Run tests with: dotnet test
/// 
/// Note: These tests will make real API calls to Azure and may incur costs.
/// </summary>
public class OcrServiceIntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITestOutputHelper _output;
    private readonly IOcrService _ocrService;
    private readonly bool _azureConfigured;
    private readonly List<Competitor> _testCompetitors;
    private readonly Guid _testClubId;
    private readonly Guid _testFleetId;

    public OcrServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json", optional: true)
          .AddEnvironmentVariables()
        .Build();

        // Setup DI container
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddHttpClient();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Create mock for ICompetitorService
        var mockCompetitorService = new Mock<SailScores.Web.Services.Interfaces.ICompetitorService>();
        // Create mock for IMatchingService
        var mockMatchingService = new Mock<IMatchingService>();
        // Optionally, setup default behavior for GetSuggestions if needed
        mockMatchingService.Setup(m => m.GetSuggestions(It.IsAny<string>(), It.IsAny<IEnumerable<Competitor>>()))
 .Returns(Enumerable.Empty<MatchingSuggestion>());
        
        // Configure mock to return test competitors for specific club/fleet
        _testClubId = new Guid("11111111-1111-1111-1111-111111111111");
        _testFleetId = new Guid("22222222-2222-2222-2222-222222222222");
        _testCompetitors = new List<Competitor>
        {
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA44", AlternativeSailNumber = "ABC123" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA51", AlternativeSailNumber = null },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA327", AlternativeSailNumber = "XYZ789" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA340", AlternativeSailNumber = "XYZ789" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA334", AlternativeSailNumber = "XYZ789" },
            new Competitor { Id = Guid.NewGuid(), SailNumber = "USA339", AlternativeSailNumber = "XYZ789" }
        };
        
        mockCompetitorService
            .Setup(s => s.GetCompetitorsForFleetAsync(_testClubId, _testFleetId))
            .ReturnsAsync(new Dictionary<string, IEnumerable<Competitor>>
            {
                { "TestFleet", _testCompetitors }
            });

        // Register services
        services.AddScoped<SailScores.Web.Services.Interfaces.ICompetitorService>(_ => mockCompetitorService.Object);
        services.AddScoped<IMatchingService>(_ => mockMatchingService.Object); // Register the mock
        services.AddScoped<IOcrService, OcrService>();

        _serviceProvider = services.BuildServiceProvider();
        _ocrService = _serviceProvider.GetRequiredService<IOcrService>();

        // Check if Azure is configured
        var endpoint = configuration["Azure:ComputerVision:Endpoint"];
        var subscriptionKey = configuration["Azure:ComputerVision:SubscriptionKey"];
        _azureConfigured = !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(subscriptionKey);

        if (!_azureConfigured)
        {
            _output.WriteLine("WARNING: Azure Computer Vision not configured. Integration tests will be skipped.");
            _output.WriteLine("Set Azure:ComputerVision:Endpoint and Azure:ComputerVision:SubscriptionKey");
        }
    }

    [Fact]
    public void ValidateImage_WithNullImage_ReturnsError()
    {
        // Arrange
        IFormFile? nullImage = null;

        // Act
        var error = _ocrService.ValidateImage(nullImage!);

        // Assert
        Assert.NotNull(error);
        Assert.Contains("No image file provided", error);
    }

    [Fact]
    public void ValidateImage_WithInvalidFileType_ReturnsError()
    {
        // Arrange
        var image = CreateMockFormFile("test.txt", "text/plain", new byte[] { 1, 2, 3 });

        // Act
        var error = _ocrService.ValidateImage(image);

        // Assert
        Assert.NotNull(error);
        Assert.Contains("Invalid file type", error);
    }

    [Fact]
    public void ValidateImage_WithTooLargeFile_ReturnsError()
    {
        // Arrange
        var largeData = new byte[5 * 1024 * 1024]; // 5MB
        var image = CreateMockFormFile("large.jpg", "image/jpeg", largeData);

        // Act
        var error = _ocrService.ValidateImage(image);

        // Assert
        Assert.NotNull(error);
        Assert.Contains("too large", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateImage_WithValidJpeg_ReturnsNoError()
    {
        // Arrange
        var image = CreateMockFormFile("test.jpg", "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF });

        // Act
        var error = _ocrService.ValidateImage(image);

        // Assert
        Assert.Null(error);
    }

    [Fact]
    public void ValidateImage_WithValidPng_ReturnsNoError()
    {
        // Arrange
        var image = CreateMockFormFile("test.png", "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        // Act
        var error = _ocrService.ValidateImage(image);

        // Assert
        Assert.Null(error);
    }

    [Fact]
    public async Task AnalyzeImageAsync_WithSampleImage_ReturnsResults()
    {
        // Skip if Azure not configured
        if (!_azureConfigured)
        {
            _output.WriteLine("Skipping: Azure Computer Vision not configured");
            return;
        }

        // Arrange
        var testImagePath = Path.Combine("TestImages", "sample-sail-numbers.jpg");
        
        if (!System.IO.File.Exists(testImagePath))
        {
            _output.WriteLine($"Skipping: Test image not found at {testImagePath}");
            _output.WriteLine("Place test images in TestImages/ directory to run this test");
            return;
        }

        var imageBytes = await System.IO.File.ReadAllBytesAsync(testImagePath);
        var image = CreateMockFormFile("sample.jpg", "image/jpeg", imageBytes);

        // Act
        var result = await _ocrService.AnalyzeImageAsync(image, null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OcrMatchResult>(result);
        
        var matchResult = result as OcrMatchResult;
        Assert.NotNull(matchResult);
        
        // Without competitors, should have no line suggestions
        Assert.Empty(matchResult.Lines);
        //Assert.NotEmpty(matchResult.UnmatchedText);
        
        _output.WriteLine("OCR Result:");
        _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }

    [Fact]
    public async Task AnalyzeImageAsync_WithContextHints_ReturnsResults()
    {
        // Skip if Azure not configured
        if (!_azureConfigured)
        {
            _output.WriteLine("Skipping: Azure Computer Vision not configured");
            return;
        }

        var testImagePath = Path.Combine("TestImages", "sample-sail-numbers.jpg");
        
        if (!System.IO.File.Exists(testImagePath))
        {
            _output.WriteLine($"Skipping: Test image not found at {testImagePath}");
            return;
        }

        // Arrange
        var imageBytes = await System.IO.File.ReadAllBytesAsync(testImagePath);
        var image = CreateMockFormFile("sample.jpg", "image/jpeg", imageBytes);
        var contextHints = new[] { "123", "456", "789", "USA 42", "K-7" };

        // Act
        var result = await _ocrService.AnalyzeImageAsync(image, null, null, contextHints);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OcrMatchResult>(result);
        
        var matchResult = result as OcrMatchResult;
        Assert.NotNull(matchResult);
        
        // Without competitors, hints don't affect matching, should have unmatched text
        Assert.Empty(matchResult.Lines);
        
        _output.WriteLine("OCR Result with context hints:");
        _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine($"\nContext hints provided: {string.Join(", ", contextHints)}");
    }

    [Fact]
    public async Task AnalyzeImageAsync_WithInvalidConfiguration_ThrowsException()
    {
        // Arrange - Create service with invalid configuration
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Azure:ComputerVision:Endpoint"] = "",
                ["Azure:ComputerVision:SubscriptionKey"] = ""
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(invalidConfig);
        services.AddHttpClient();
        services.AddLogging();
        services.AddScoped<IOcrService, OcrService>();

        var serviceProvider = services.BuildServiceProvider();
        var ocrService = serviceProvider.GetRequiredService<IOcrService>();

        var image = CreateMockFormFile("test.jpg", "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _ocrService.AnalyzeImageAsync(image, null, null, null));
    }

    private static IFormFile CreateMockFormFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "image", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
