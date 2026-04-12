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
    }
}
