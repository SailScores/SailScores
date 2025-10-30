using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using SailScores.Web.Services;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Text;

namespace SailScores.Web.Areas.Api.Controllers
{
    [ApiController]
    [Route("api/stripe/webhook")]
    [AllowAnonymous]
    public class StripeWebhookController : ControllerBase
    {
        private readonly ILogger<StripeWebhookController> _logger;
        private readonly IStripeService _stripeService;

        public StripeWebhookController(
            ILogger<StripeWebhookController> logger,
            IStripeService stripeService)
        {
            _logger = logger;
            _stripeService = stripeService;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
            {
                // Enable buffering so the request body can be read multiple times
                Request.EnableBuffering();

                // Read the raw request body
                string json;
                using (var reader = new StreamReader(
                    Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true))
                {
                    json = await reader.ReadToEndAsync();
                    // Reset the stream position for potential re-reads
                    Request.Body.Position = 0;
                }
                
                // Get the Stripe signature from headers
                var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

                _logger.LogInformation($"Stripe webhook received. Body length: {json?.Length ?? 0}, Has signature: {!string.IsNullOrWhiteSpace(stripeSignature)}");

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("Stripe webhook received empty body");
                    return BadRequest("Empty request body");
                }

                if (string.IsNullOrWhiteSpace(stripeSignature))
                {
                    _logger.LogWarning("Stripe webhook received without signature");
                    return BadRequest("Missing Stripe-Signature header");
                }

                await _stripeService.HandleStripeWebhookAsync(json, stripeSignature, _logger);
                
                _logger.LogInformation("Stripe webhook processed successfully");
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Null argument in Stripe webhook processing: {ParameterName}", ex.ParamName);
                return BadRequest($"Invalid request: {ex.ParamName} is null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                return BadRequest("Error processing webhook");
            }

            return Ok();
        }
    }
}
