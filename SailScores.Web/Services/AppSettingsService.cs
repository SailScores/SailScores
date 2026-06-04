using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
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
    /// Gets a relative URI when the current host matches the preferred host, otherwise returns an absolute URI.
    /// Automatically strips recursive returnUrl parameters to prevent URL bloat.
    /// </summary>
    /// <param name="request">The current HTTP request</param>
    /// <returns>
    /// A relative URI (e.g., /path?query=value) if no host redirect is needed,
    /// or an absolute URI if the preferred host differs from the current host.
    /// </returns>
    /// <remarks>
    /// This method is ideal for same-site navigation returnUrl parameters (login, edit actions, etc.).
    /// It prevents recursive returnUrl nesting like /login?returnUrl=/edit?returnUrl=/login.
    /// Use <see cref="GetPreferredUri"/> when you always need an absolute URL (emails, external APIs).
    /// </remarks>
    public string GetRelativeUri(HttpRequest request)
    {
        var preferredHost = _config["PreferredHost"];
        var currentHost = request.Host.ToUriComponent();

        var cleanedQueryString = StripRecursiveReturnUrl(request.QueryString.Value);

        if (string.IsNullOrWhiteSpace(preferredHost) || 
            string.Equals(preferredHost, currentHost, StringComparison.OrdinalIgnoreCase))
        {
            var relativeUri = string.Concat(
                request.PathBase.ToUriComponent(),
                request.Path.ToUriComponent(),
                cleanedQueryString);

            return relativeUri;
        }

        var absoluteUri = string.Concat(
            request.Scheme,
            "://",
            preferredHost,
            request.PathBase.ToUriComponent(),
            request.Path.ToUriComponent(),
            cleanedQueryString);

        return absoluteUri;
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

    private static string StripRecursiveReturnUrl(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return queryString;
        }

        var query = QueryHelpers.ParseQuery(queryString);
        if (!query.TryGetValue("returnUrl", out var returnUrlValue))
        {
            return queryString;
        }

        var returnUrl = returnUrlValue.ToString();
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return queryString;
        }

        var returnUrlParts = returnUrl.Split('?');
        if (returnUrlParts.Length <= 1)
        {
            return queryString;
        }

        var nestedQuery = QueryHelpers.ParseQuery(returnUrlParts[1]);
        if (nestedQuery.ContainsKey("returnUrl"))
        {
            var simplifiedReturnUrl = returnUrlParts[0];
            var queryParams = new Dictionary<string, string>();

            foreach (var kvp in query)
            {
                if (kvp.Key == "returnUrl")
                {
                    queryParams[kvp.Key] = simplifiedReturnUrl;
                }
                else
                {
                    queryParams[kvp.Key] = kvp.Value.ToString();
                }
            }

            return QueryHelpers.AddQueryString(string.Empty, queryParams).TrimStart('?');
        }

        return queryString;
    }
}
