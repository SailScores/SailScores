using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Areas.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OcrController : ControllerBase
{
    private readonly IAuthorizationService _authService;
    private readonly IOcrService _ocrService;
    private readonly ILogger<OcrController> _logger;

    public OcrController(
        IAuthorizationService authService,
        IOcrService ocrService,
        ILogger<OcrController> logger)
    {
        _authService = authService;
        _ocrService = ocrService;
        _logger = logger;
    }

    /// <summary>
    /// Analyze an image to extract text using Azure Computer Vision OCR
    /// </summary>
    /// <param name="image">Image file (JPEG or PNG, max 4MB)</param>
    /// <param name="clubId">Optional clubId to use with fleetId for context hints</param>
    /// <param name="fleetId">Optional fleetId for building context hints of expected sail numbers</param>
    /// <param name="hints">Optional explicit hints to influence matching downstream</param>
    /// <returns>OCR results</returns>
    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AnalyzeImage(
        [FromForm] IFormFile image,
        [FromForm] Guid? clubId = null,
        [FromForm] Guid? fleetId = null,
        [FromForm] string[]? hints = null)
    {
        // Validate file
        var validationError = _ocrService.ValidateImage(image);
        if (validationError != null)
        {
            return BadRequest(new { error = validationError });
        }

        try
        {
            var result = await _ocrService.AnalyzeImageAsync(image, clubId, fleetId, hints);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "OCR service configuration error");
            return StatusCode(500, new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Azure Computer Vision service");
            return StatusCode(503, new { error = $"OCR service unavailable: {ex.Message}" });
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout waiting for OCR results");
            return StatusCode(504, new { error = "OCR processing timed out. Please try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing OCR request");
            return StatusCode(500, new { error = $"Error processing image: {ex.Message}" });
        }
    }
}
