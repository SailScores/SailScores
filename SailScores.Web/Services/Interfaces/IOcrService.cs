using Microsoft.AspNetCore.Http;

namespace SailScores.Web.Services.Interfaces;

/// <summary>
/// Service for OCR (Optical Character Recognition) operations using Azure Computer Vision
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Analyze an image and return matched competitors with confidence scores
    /// </summary>
    /// <param name="image">Image file (JPEG or PNG, max 4MB)</param>
    /// <param name="clubId">Optional club id to resolve fleet competitors</param>
    /// <param name="fleetId">Optional fleet id to resolve competitor sail numbers</param>
    /// <param name="explicitHints">Optional explicit hints to merge with fleet hints</param>
    Task<OcrMatchResult> AnalyzeImageAsync(IFormFile image, Guid? clubId, Guid? fleetId, IEnumerable<string>? explicitHints = null);

    /// <summary>
    /// Validate that an image file is acceptable for OCR processing
    /// </summary>
    /// <param name="image">Image file to validate</param>
    /// <returns>Error message if invalid, null if valid</returns>
    string? ValidateImage(IFormFile image);
}

/// <summary>
/// Result of OCR analysis with matched competitors
/// </summary>
public class OcrMatchResult
{
    /// <summary>
    /// Per-line OCR results with ordered suggestions (best first)
    /// </summary>
    public IEnumerable<OcrLineMatch> Lines { get; set; } = Array.Empty<OcrLineMatch>();
}

/// <summary>
/// Matches for a single OCR line, suggestions ordered by confidence (best first)
/// </summary>
public class OcrLineMatch
{
    /// <summary>
    /// The original OCR text line
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Ordered suggestions for this text (best first)
    /// </summary>
    public IEnumerable<OcrCompetitorMatch> Suggestions { get; set; } = Array.Empty<OcrCompetitorMatch>();

    /// <summary>
    /// The outline bounding box of the text as [x1, y1, x2, y2, ...] (from Azure OCR, normalized 0-1 or pixel coordinates)
    /// </summary>
    public double[]? BoundingBox { get; set; }

    /// <summary>
    /// Convenience property for the top suggestion
    /// </summary>
    public OcrCompetitorMatch? Best => Suggestions?.FirstOrDefault();
}

/// <summary>
/// A competitor matched from OCR text
/// </summary>
public class OcrCompetitorMatch
{
    /// <summary>
    /// The matched competitor
    /// </summary>
    public required SailScores.Core.Model.Competitor Competitor { get; set; }

    /// <summary>
    /// Confidence score (0-1) of the match
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// The OCR text that was matched
    /// </summary>
    public required string MatchedText { get; set; }

    /// <summary>
    /// Whether this was an exact match on sail number
    /// </summary>
    public bool IsExactMatch { get; set; }

    /// <summary>
    /// Whether any matches were found for the OCR line (true when Suggestions is non-empty)
    /// </summary>
    public bool HasMatches { get; set; }
}
