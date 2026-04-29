using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos.Public;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Areas.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/public/v1/clubs/{clubInitials}/races")]
    public class PublicRaceController : PublicApiControllerBase
    {
        private readonly IPublicApiService _publicApiService;

        public PublicRaceController(IPublicApiService publicApiService)
        {
            _publicApiService = publicApiService;
        }

        [HttpGet("{raceId}")]
        [ProducesResponseType(typeof(PublicRaceDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PublicRaceDetailResponseDto>> Get(
            [FromRoute] string clubInitials,
            [FromRoute] Guid raceId)
        {
            var dto = await _publicApiService.GetRaceDetailAsync(clubInitials, raceId);
            if (dto == null)
            {
                return NotFoundProblem("Race was not found for the provided route.", "race_not_found");
            }

            SetPublicCacheHeaders(300);

            return Ok(dto);
        }
    }
}
