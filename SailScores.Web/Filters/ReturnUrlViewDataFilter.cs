using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SailScores.Web.Services;

namespace SailScores.Web.Filters;

public sealed class ReturnUrlViewDataFilter : IActionFilter
{
    public const string ReturnUrlKey = "ReturnUrl";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is not Controller controller)
        {
            return;
        }

        string returnUrl = null;

        if (context.ActionArguments.TryGetValue("returnUrl", out var actionValue)
            && actionValue is string actionReturnUrl)
        {
            // Normalize the argument itself, so actions that set ViewData or redirect see the clean value
            returnUrl = ReturnUrlNormalizer.Normalize(actionReturnUrl);
            context.ActionArguments["returnUrl"] = returnUrl;
        }

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            returnUrl = context.HttpContext.Request.Query["returnUrl"].ToString();
        }

        if (string.IsNullOrWhiteSpace(returnUrl) && context.HttpContext.Request.HasFormContentType)
        {
            returnUrl = context.HttpContext.Request.Form["returnUrl"].ToString();
        }

        controller.ViewData[ReturnUrlKey] = ReturnUrlNormalizer.Normalize(returnUrl);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
