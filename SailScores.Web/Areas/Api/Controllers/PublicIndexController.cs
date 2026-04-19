using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos.Public;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Areas.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/public/v1")]
    public class PublicIndexController : PublicApiControllerBase
    {
        private readonly IPublicApiService _publicApiService;

        public PublicIndexController(IPublicApiService publicApiService)
        {
            _publicApiService = publicApiService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PublicApiRootResponseDto), StatusCodes.Status200OK)]
        public ActionResult<PublicApiRootResponseDto> Get()
        {
            SetPublicCacheHeaders(300);

            return Ok(_publicApiService.GetRootResponse());
        }

        [HttpGet("clubs")]
        [ProducesResponseType(typeof(PublicListResponseDto<PublicClubListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<Dictionary<string, string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetClubs(
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string format)
        {
            var pagingValidation = ValidatePaging(page, pageSize);
            if (pagingValidation != null)
            {
                return pagingValidation;
            }

            var response = await _publicApiService.GetClubsAsync(page, pageSize);
            SetPublicCacheHeaders(300, "page", "pageSize", "format");

            var options = response.Items
                .Select(i => (Key: i.Name, Value: i.ClubInitials))
                .OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (IsOptionsFormat(format))
            {
                return Ok(BuildJsonOptions(options));
            }

            return Ok(response);
        }

        [HttpGet("clubs/{clubToken}")]
        [ProducesResponseType(typeof(PublicClubDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PublicClubDetailResponseDto>> GetClub([FromRoute] string clubToken)
        {
            var response = await _publicApiService.GetClubAsync(clubToken);
            if (response == null)
            {
                return NotFoundProblem("Club was not found for the provided route.", "club_not_found");
            }

            SetPublicCacheHeaders(300);
            return Ok(response);
        }

        [HttpGet("clubs/{clubToken}/seasons")]
        [ProducesResponseType(typeof(PublicListResponseDto<PublicSeasonListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<Dictionary<string, string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetSeasons(
            [FromRoute] string clubToken,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string format)
        {
            var pagingValidation = ValidatePaging(page, pageSize);
            if (pagingValidation != null)
            {
                return pagingValidation;
            }

            var response = await _publicApiService.GetSeasonsAsync(clubToken, page, pageSize);
            if (response == null)
            {
                return NotFoundProblem("Club was not found for the provided route.", "club_not_found");
            }

            SetPublicCacheHeaders(300, "page", "pageSize", "format");

            var options = response.Items
                .Select(i => (Key: i.SeasonName, Value: i.SeasonUrlName))
                .OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (IsOptionsFormat(format))
            {
                return Ok(BuildJsonOptions(options));
            }

            return Ok(response);
        }

        [HttpGet("clubs/{clubToken}/series")]
        [ProducesResponseType(typeof(PublicListResponseDto<PublicSeriesListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<Dictionary<string, string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetSeriesByClub(
            [FromRoute] string clubToken,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string format)
        {
            var pagingValidation = ValidatePaging(page, pageSize);
            if (pagingValidation != null)
            {
                return pagingValidation;
            }

            var response = await _publicApiService.GetSeriesAsync(clubToken, null, page, pageSize);
            if (response == null)
            {
                return NotFoundProblem("Club was not found for the provided route.", "club_not_found");
            }

            SetPublicCacheHeaders(300, "page", "pageSize", "format");

            var options = response.Items
                .Select(i => (Key: i.SeriesName, Value: i.SeriesUrlName))
                .OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (IsOptionsFormat(format))
            {
                return Ok(BuildJsonOptions(options));
            }

            return Ok(response);
        }

        [HttpGet("clubs/{clubToken}/seasons/{seasonUrlName}/series")]
        [ProducesResponseType(typeof(PublicListResponseDto<PublicSeriesListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<Dictionary<string, string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetSeriesBySeason(
            [FromRoute] string clubToken,
            [FromRoute] string seasonUrlName,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string format)
        {
            var pagingValidation = ValidatePaging(page, pageSize);
            if (pagingValidation != null)
            {
                return pagingValidation;
            }

            var response = await _publicApiService.GetSeriesAsync(clubToken, seasonUrlName, page, pageSize);
            if (response == null)
            {
                return NotFoundProblem("Club was not found for the provided route.", "club_not_found");
            }

            SetPublicCacheHeaders(300, "page", "pageSize", "format");

            var options = response.Items
                .Select(i => (Key: i.SeriesName, Value: i.SeriesUrlName))
                .OrderBy(i => i.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (IsOptionsFormat(format))
            {
                return Ok(BuildJsonOptions(options));
            }

            return Ok(response);
        }

        private static IEnumerable<Dictionary<string, string>> BuildJsonOptions(
            IEnumerable<(string Key, string Value)> options)
        {
            return options.Select(o => new Dictionary<string, string>
            {
                [o.Key ?? string.Empty] = o.Value ?? string.Empty
            });
        }

        private static bool IsOptionsFormat(string format)
        {
            return string.Equals(format, "options", StringComparison.OrdinalIgnoreCase);
        }

        private ActionResult ValidatePaging(int? page, int? pageSize)
        {
            if (!page.HasValue && !pageSize.HasValue)
            {
                return null;
            }

            if (!page.HasValue || !pageSize.HasValue)
            {
                return BadRequestProblem("Both page and pageSize are required when paging.", "invalid_paging");
            }

            if (page.Value <= 0 || pageSize.Value <= 0)
            {
                return BadRequestProblem("page and pageSize must be greater than zero.", "invalid_paging");
            }

            return null;
        }
    }
}
