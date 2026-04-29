using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCaching;

namespace SailScores.Web.Areas.Api.Controllers
{
    [ApiController]
    public abstract class PublicApiControllerBase : ControllerBase
    {
        protected void SetPublicCacheHeaders(int maxAgeSeconds, params string[] varyByQueryKeys)
        {
            Response.Headers["Cache-Control"] = $"public,max-age={maxAgeSeconds}";

            if (varyByQueryKeys != null && varyByQueryKeys.Length > 0)
            {
                var responseCachingFeature = HttpContext?.Features?.Get<IResponseCachingFeature>();
                if (responseCachingFeature != null)
                {
                    responseCachingFeature.VaryByQueryKeys = varyByQueryKeys;
                }
            }
        }

        protected ActionResult BadRequestProblem(string detail, string errorCode)
        {
            return ProblemResponse(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: detail,
                errorCode: errorCode);
        }

        protected ActionResult NotFoundProblem(string detail, string errorCode)
        {
            return ProblemResponse(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: detail,
                errorCode: errorCode);
        }

        protected ActionResult TooManyRequestsProblem(string detail, string errorCode)
        {
            return ProblemResponse(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too Many Requests",
                detail: detail,
                errorCode: errorCode);
        }

        protected ActionResult InternalServerErrorProblem(string detail, string errorCode)
        {
            return ProblemResponse(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Server Error",
                detail: detail,
                errorCode: errorCode);
        }

        protected ActionResult ProblemResponse(int statusCode, string title, string detail, string errorCode)
        {
            var problem = new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{statusCode}",
                Title = title,
                Status = statusCode,
                Detail = detail,
                Instance = HttpContext?.Request?.Path.Value
            };

            problem.Extensions["traceId"] = HttpContext?.TraceIdentifier;
            problem.Extensions["errorCode"] = errorCode;

            return StatusCode(statusCode, problem);
        }
    }
}
