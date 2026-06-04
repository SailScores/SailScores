using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class RedirectHelper : IRedirectHelper
{
    /// <summary>
    /// Safely redirects to a return URL if it's local, otherwise redirects to a default action.
    /// This prevents open redirect vulnerabilities by validating the return URL using IsLocalUrl
    /// for relative URLs and host comparison for absolute URLs.
    /// </summary>
    /// <param name="urlHelper">The URL helper from the controller</param>
    /// <param name="request">The HTTP request to get the current host</param>
    /// <param name="returnUrl">The user-supplied return URL to validate</param>
    /// <param name="defaultAction">The default action to redirect to if returnUrl is invalid</param>
    /// <param name="defaultController">The default controller to redirect to if returnUrl is invalid</param>
    /// <returns>An ActionResult that redirects to a validated local URL</returns>
    public ActionResult SafeRedirect(
        IUrlHelper urlHelper,
        HttpRequest request,
        string returnUrl,
        string defaultAction,
        string defaultController)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return new RedirectToActionResult(defaultAction, defaultController, null);
        }

        // Check if it's a relative URL (most common case)
        if (urlHelper.IsLocalUrl(returnUrl))
        {
            return new RedirectResult(returnUrl);
        }

        // Check if it's an absolute URL pointing to the same host
        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            var requestHost = request.Host.Host;
            var requestPort = request.Host.Port;
            var requestScheme = request.Scheme;

            // Validate scheme, host, and port match
            if (string.Equals(uri.Scheme, requestScheme, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(uri.Host, requestHost, StringComparison.OrdinalIgnoreCase) &&
                (uri.Port == requestPort || 
                 (uri.Port == -1 && ((requestScheme == "https" && requestPort == 443) || 
                                     (requestScheme == "http" && requestPort == 80)))))
            {
                return new RedirectResult(returnUrl);
            }
        }

        // If validation fails, redirect to default
        return new RedirectToActionResult(defaultAction, defaultController, null);
    }
}
