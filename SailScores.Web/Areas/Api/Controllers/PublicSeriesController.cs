using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos.Public;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Areas.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/public/v1/clubs/{clubInitials}/seasons/{seasonUrlName}/series")]
    public class PublicSeriesController : PublicApiControllerBase
    {
        private readonly IPublicApiService _publicApiService;

        public PublicSeriesController(IPublicApiService publicApiService)
        {
            _publicApiService = publicApiService;
        }

        [HttpGet("{seriesUrlName}")]
        [ProducesResponseType(typeof(PublicSeriesDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PublicSeriesDetailResponseDto>> Get(
            [FromRoute] string clubInitials,
            [FromRoute] string seasonUrlName,
            [FromRoute] string seriesUrlName,
            [FromQuery] string include,
            [FromQuery] int? raceCount)
        {
            if (string.IsNullOrWhiteSpace(clubInitials)
                || string.IsNullOrWhiteSpace(seasonUrlName)
                || string.IsNullOrWhiteSpace(seriesUrlName))
            {
                return BadRequestProblem("Club, season, and series route values are required.", "invalid_route_values");
            }

            if (raceCount.HasValue && raceCount.Value < 1)
            {
                return BadRequestProblem("raceCount must be greater than zero.", "invalid_race_count");
            }

            var includeValues = (include ?? string.Empty)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var includeCompetitors = includeValues.Contains("competitors", StringComparer.OrdinalIgnoreCase);
            var includeRaces = includeValues.Contains("races", StringComparer.OrdinalIgnoreCase);
            var dto = await _publicApiService.GetSeriesDetailAsync(
                clubInitials,
                seasonUrlName,
                seriesUrlName,
                includeCompetitors,
                includeRaces,
                raceCount);
            if (dto == null)
            {
                return NotFoundProblem("Series was not found for the provided route.", "series_not_found");
            }

            SetPublicCacheHeaders(300, "include", "raceCount");

            return Ok(dto);
        }
    }
}
