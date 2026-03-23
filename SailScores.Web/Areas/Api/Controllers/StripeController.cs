using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SailScores.Web.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using SailScores.Web.Authorization;

namespace SailScores.Web.Areas.Api.Controllers
{
    [ApiController]
    [Route("api/stripe")]
    public class StripeController : ControllerBase
    {
        private readonly IStripeService _stripeService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeController> _logger;
        private readonly AppSettingsService _appSettingsService;

        public StripeController(IStripeService stripeService, IConfiguration configuration, ILogger<StripeController> logger, AppSettingsService appSettingsService)
        {
            _stripeService = stripeService;
            _configuration = configuration;
            _logger = logger;
            _appSettingsService = appSettingsService;
        }

        public class CreateCheckoutSessionRequest
        {
            public string Plan { get; set; }
            public string ClubId { get; set; }
            public string ClubInitials { get; set; }
            public string UserEmail { get; set; }
        }

        [HttpGet("validate-configuration")]
        [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
        public async Task<IActionResult> ValidateConfiguration()
        {
            var validationResult = await _stripeService.ValidateConfigurationAsync();

            if (validationResult.IsValid)
            {
                return Ok(validationResult);
            }

            return StatusCode(500, validationResult);
        }

        [HttpPost("create-checkout-session")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            // Try to get email from JWT claims or fallback to request.UserEmail
            var email = User?.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                email = User?.Identity?.Name;
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                email = request.UserEmail;
            }

            var domain = _appSettingsService.GetPreferredBase(Request);
            try
            {
                var session = await _stripeService.CreateCheckoutSessionAsync(
                    request.Plan,
                    email,
                    domain
                );
                return new JsonResult(new { id = session.Id });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe API error while creating checkout session for plan '{Plan}'.", request.Plan);
                return StatusCode(502, new { error = "Payment provider error. Please try again later." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Stripe configuration error while creating checkout session.");
                return StatusCode(500, new { error = "Payment configuration error. Please contact support." });
            }
        }
    }
}
