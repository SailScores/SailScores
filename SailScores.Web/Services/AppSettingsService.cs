using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace SailScores.Web.Services;

public class AppSettingsService
{
    public const string ExternalIdentityProvidersEnabledKey =
        "Features:ExternalIdentityProviders";

    private readonly IConfiguration _config;

    public AppSettingsService(
        IConfiguration config)
    {
        _config = config;
    }

    public bool IsExternalAuthenticationEnabled()
    {
        return _config.GetValue<bool?>(ExternalIdentityProvidersEnabledKey) == true;
    }

    public string GetJwtKey()
    {
        return _config["JwtKey"];
    }

    public string GetJwtIssuer()
    {
        return _config["JwtIssuer"];
    }

    public double GetJwtExpireDays()
    {
        return _config.GetValue<double>("JwtExpireDays");
    }

    /// <summary>
    /// Gets the absolute URI for the current request, applying the preferred host if configured.
    /// </summary>
    /// <param name="request">The current HTTP request</param>
    /// <returns>An absolute URI (e.g., https://example.com/path?query=value)</returns>
    /// <remarks>
    /// Use this method when you need an absolute URL, such as in emails, external APIs, or RSS feeds.
    /// For same-site returnUrl navigation, consider using <see cref="GetRelativeUri"/> instead.
    /// </remarks>
    public string GetPreferredUri(HttpRequest request)
    {
        var preferredhost = _config["PreferredHost"];
        if (!String.IsNullOrWhiteSpace(preferredhost))
        {
            request.Host = new HostString(preferredhost);
        }

        var absoluteUri = string.Concat(
            request.Scheme,
            "://",
            request.Host.ToUriComponent(),
            request.PathBase.ToUriComponent(),
            request.Path.ToUriComponent(),
            request.QueryString.ToUriComponent());

        return absoluteUri;
    }

    /// <summary>
    /// Gets the current page as a returnUrl for login links: a relative URI when the current host
    /// matches the preferred host, otherwise an absolute URI.
    /// </summary>
    /// <param name="request">The current HTTP request</param>
    /// <returns>
    /// A relative URI (e.g., /path?query=value) if no host redirect is needed,
    /// or an absolute URI if the preferred host differs from the current host.
    /// On /Account pages, returns the page that page would return to (or null), so links between
    /// Login and Register don't nest returnUrls (/Account/Login?returnUrl=/Account/Register?returnUrl=...).
    /// </returns>
    /// <remarks>
    /// Use <see cref="GetPreferredUri"/> when you always need an absolute URL (emails, external APIs).
    /// </remarks>
    public string GetRelativeUri(HttpRequest request)
    {
        if (ReturnUrlNormalizer.IsAccountPath(request.Path.Value))
        {
            return ReturnUrlNormalizer.Normalize(request.Query["returnUrl"].ToString());
        }

        var preferredHost = _config["PreferredHost"];
        var currentHost = request.Host.ToUriComponent();

        var pathAndQuery = string.Concat(
            request.PathBase.ToUriComponent(),
            request.Path.ToUriComponent(),
            request.QueryString.ToUriComponent());

        if (string.IsNullOrWhiteSpace(preferredHost) ||
            string.Equals(preferredHost, currentHost, StringComparison.OrdinalIgnoreCase))
        {
            return pathAndQuery;
        }

        return string.Concat(request.Scheme, "://", preferredHost, pathAndQuery);
    }

    public string GetPreferredBase(HttpRequest request)
    {
        var preferredhost = _config["PreferredHost"];
        if (!String.IsNullOrWhiteSpace(preferredhost))
        {
            request.Host = new HostString(preferredhost);
        }

        var absoluteUri = string.Concat(
            request.Scheme,
            "://",
            request.Host.ToUriComponent(),
            request.PathBase.ToUriComponent());

        return absoluteUri;
    }
}
