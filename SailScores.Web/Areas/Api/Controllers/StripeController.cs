using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SailScores.Web.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SailScores.Web.Areas.Api.Controllers
{
    [ApiController]
    [Route("api/stripe")]
    public class StripeController : ControllerBase
    {
        private readonly IStripeService _stripeService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeController> _logger;

        public StripeController(IStripeService stripeService, IConfiguration configuration, ILogger<StripeController> logger)
        {
            _stripeService = stripeService;
            _configuration = configuration;
            _logger = logger;
        }

        public class CreateCheckoutSessionRequest
        {
            public string Plan { get; set; }
            public string ClubId { get; set; }
            public string ClubInitials { get; set; }
            public string UserEmail { get; set; }
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

            var domain = _configuration["App:BaseUrl"] ?? (Request.Scheme + "://" + Request.Host.Value);
            var session = await _stripeService.CreateCheckoutSessionAsync(
                request.Plan,
                email,
                domain
            );
            return new JsonResult(new { id = session.Id });
        }
    }
}
