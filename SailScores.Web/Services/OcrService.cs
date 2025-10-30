using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SailScores.Core.Services;
using SailScores.Web.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Competitor = SailScores.Core.Model.Competitor;

namespace SailScores.Web.Services;

/// <summary>
/// Service for OCR (Optical Character Recognition) operations using Azure Computer Vision
/// </summary>
public class OcrService : IOcrService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OcrService> _logger;
    private readonly Core.Services.ICompetitorService _competitorService;
    private readonly IMatchingService _matchingService;
    private const long MaxFileSize = 4 * 1024 * 1024; // 4MB
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/jpg" };

    public OcrService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OcrService> logger,
        Core.Services.ICompetitorService competitorService,
        IMatchingService matchingService)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _competitorService = competitorService;
        _matchingService = matchingService;
    }

    /// <summary>
    /// Validate that an image file is acceptable for OCR processing
    /// </summary>
    public string? ValidateImage(IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            return "No image file provided";
        }

        if (image.Length > MaxFileSize)
        {
            return $"File too large. Maximum size is {MaxFileSize / 1024 / 1024}MB";
        }

        if (!Array.Exists(AllowedContentTypes, t => string.Equals(t, image.ContentType, StringComparison.OrdinalIgnoreCase)))
        {
            return "Invalid file type. Only JPEG and PNG are supported.";
        }

        return null;
    }

    /// <summary>
    /// Analyze an image to extract text using Azure Computer Vision OCR
    /// </summary>
    public async Task<OcrMatchResult> AnalyzeImageAsync(IFormFile image, Guid? clubId, Guid? fleetId, IEnumerable<string>? explicitHints = null)
    {
        IEnumerable<Competitor> competitors = Array.Empty<Competitor>();

        // If club and fleet IDs are provided, get the competitors
        if (clubId.HasValue && fleetId.HasValue)
        {
            try
            {
                competitors = await _competitorService.GetCompetitorsAsync(clubId.Value, fleetId.Value, false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to load competitors for matching. Proceeding without competitors.");
            }
        }

        // Build hints from competitors for additional context (if needed)
        IEnumerable<string>? contextHints = null;
        if (competitors.Any())
        {
            contextHints = competitors
                .SelectMany(c => new[] { c.SailNumber, c.AlternativeSailNumber })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        // If explicit hints are provided, combine them
        if (explicitHints != null)
        {
            var userHints = explicitHints.Where(h => !string.IsNullOrWhiteSpace(h));
            contextHints = contextHints == null
                ? userHints
                : contextHints.Concat(userHints).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        return await AnalyzeImageWithMatchingAsync(image, competitors);
    }

    private async Task<OcrMatchResult> AnalyzeImageWithMatchingAsync(IFormFile image, IEnumerable<Competitor> competitors)
    {
        var endpoint = _configuration["Azure:ComputerVision:Endpoint"];
        var subscriptionKey = _configuration["Azure:ComputerVision:SubscriptionKey"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(subscriptionKey))
        {
            _logger.LogError("Azure Computer Vision not configured. Missing endpoint or subscription key.");
            throw new InvalidOperationException("OCR service not configured");
        }

        _logger.LogInformation("Processing OCR request for image: {FileName}, Size: {FileSize} bytes",
                   image.FileName, image.Length);

        // Get OCR text and bounding boxes from Azure
        var ocrLineResults = await CallAzureComputerVisionV4WithBoxesAsync(endpoint, subscriptionKey, image);

        // Perform matching via core matching service
        var lineMatches = new List<OcrLineMatch>();

        foreach (var lineResult in ocrLineResults)
        {
            var text = lineResult.Text;
            var boundingBox = lineResult.BoundingBox;
            var suggestionsCore = _matchingService.GetSuggestions(text, competitors)?.ToList() ?? new List<MatchingSuggestion>();

            var suggestions = suggestionsCore.Select(s => new OcrCompetitorMatch
            {
                Competitor = s.Competitor,
                Confidence = s.Confidence,
                MatchedText = s.MatchedText,
                IsExactMatch = s.IsExactMatch,
                HasMatches = true
            }).ToList();

            lineMatches.Add(new OcrLineMatch
            {
                Text = text,
                Suggestions = suggestions,
                BoundingBox = boundingBox
            });
        }

        return new OcrMatchResult
        {
            Lines = lineMatches
        };
    }

    private async Task<List<(string Text, double[]? BoundingBox)>> CallAzureComputerVisionV4WithBoxesAsync(
        string endpoint,
        string subscriptionKey,
        IFormFile image)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var analyzeUrl = $"{endpoint.TrimEnd('/')}/computervision/imageanalysis:analyze?api-version=2024-02-01&features=read";

        using var imageStream = image.OpenReadStream();
        using var content = new StreamContent(imageStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);

        var request = new HttpRequestMessage(HttpMethod.Post, analyzeUrl)
        {
            Content = content
        };
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        _logger.LogDebug("Submitting image to Azure Image Analysis4.0: {Url}", analyzeUrl);

        var response = await client.SendAsync(request);

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Azure Image Analysis API error: {StatusCode} - {Error}", response.StatusCode, body);
            throw new HttpRequestException($"Azure Vision API error: {response.StatusCode} - {body}");
        }

        // Parse and extract text lines and bounding boxes
        using var doc = JsonDocument.Parse(body);
        return ExtractTextLinesAndBoxesFromResponse(doc);
    }

    private List<(string Text, double[]? BoundingBox)> ExtractTextLinesAndBoxesFromResponse(JsonDocument doc)
    {
        var results = new List<(string, double[]?)>();

        if (doc.RootElement.TryGetProperty("readResult", out var readResult))
        {
            if (readResult.TryGetProperty("blocks", out var blocks) && blocks.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in blocks.EnumerateArray())
                {
                    if (block.TryGetProperty("lines", out var blockLines) && blockLines.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var line in blockLines.EnumerateArray())
                        {
                            string text = line.TryGetProperty("content", out var contentProp)
                                ? contentProp.GetString() ?? string.Empty
                                : (line.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? string.Empty : string.Empty);

                            double[]? boundingBox = null;
                            if (line.TryGetProperty("boundingPolygon", out var boundingPolygon) && boundingPolygon.ValueKind == JsonValueKind.Array)
                            {
                                var points = new List<double>();
                                foreach (var point in boundingPolygon.EnumerateArray())
                                {
                                    if (point.TryGetProperty("x", out var xProp) && point.TryGetProperty("y", out var yProp))
                                    {
                                        points.Add(xProp.GetDouble());
                                        points.Add(yProp.GetDouble());
                                    }
                                }
                                if (points.Count > 0)
                                    boundingBox = points.ToArray();
                            }
                            else if (line.TryGetProperty("boundingBox", out var boundingBoxProp) && boundingBoxProp.ValueKind == JsonValueKind.Array)
                            {
                                boundingBox = boundingBoxProp.EnumerateArray().Select(e => e.GetDouble()).ToArray();
                            }

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                results.Add((text.Trim(), boundingBox));
                            }
                        }
                    }
                }
            }
        }

        return results;
    }
}
