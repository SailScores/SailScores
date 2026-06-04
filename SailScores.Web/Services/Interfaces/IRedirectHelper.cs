using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SailScores.Web.Services.Interfaces;

public interface IRedirectHelper
{
    /// <summary>
    /// Safely redirects to a return URL if it's local, otherwise redirects to a default action.
    /// This prevents open redirect vulnerabilities by validating the return URL.
    /// Supports both relative URLs (e.g., /LHYC/series) and absolute URLs to the same host
    /// (e.g., https://localhost:5001/LHYC).
    /// </summary>
    /// <param name="urlHelper">The URL helper from the controller</param>
    /// <param name="request">The HTTP request to get the current host</param>
    /// <param name="returnUrl">The user-supplied return URL to validate</param>
    /// <param name="defaultAction">The default action to redirect to if returnUrl is invalid</param>
    /// <param name="defaultController">The default controller to redirect to if returnUrl is invalid</param>
    /// <returns>An ActionResult that redirects to a validated local URL</returns>
    ActionResult SafeRedirect(
        IUrlHelper urlHelper,
        HttpRequest request,
        string returnUrl,
        string defaultAction,
        string defaultController);
}
