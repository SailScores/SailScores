using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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

        if (context.ActionArguments.TryGetValue("returnUrl", out var actionValue))
        {
            returnUrl = actionValue as string;
        }

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            returnUrl = context.HttpContext.Request.Query["returnUrl"].ToString();
        }

        if (string.IsNullOrWhiteSpace(returnUrl) && context.HttpContext.Request.HasFormContentType)
        {
            returnUrl = context.HttpContext.Request.Form["returnUrl"].ToString();
        }

        controller.ViewData[ReturnUrlKey] = returnUrl;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
