using Microsoft.AspNetCore.WebUtilities;

namespace SailScores.Web.Services;

/// <summary>
/// Keeps returnUrl values from pointing back into /Account pages. Login and Register
/// links carry the current URL as their returnUrl, so without this each hop between
/// those pages nests another level (Login?returnUrl=Register?returnUrl=Login?...),
/// creating an endless supply of distinct URLs for crawlers to follow.
/// </summary>
public static class ReturnUrlNormalizer
{
    private const int MaxDepth = 20;

    /// <summary>
    /// Unwraps returnUrls that point at /Account pages to the page they would
    /// eventually return to. Returns null when no such page exists, so callers
    /// omit the returnUrl. Other values are returned unchanged.
    /// </summary>
    public static string Normalize(string returnUrl)
    {
        var current = returnUrl;
        for (var depth = 0; depth < MaxDepth; depth++)
        {
            if (string.IsNullOrWhiteSpace(current))
            {
                return null;
            }

            var (path, query) = SplitPathAndQuery(current);
            if (!IsAccountPath(path))
            {
                return current;
            }

            current = string.IsNullOrEmpty(query)
                ? null
                : QueryHelpers.ParseQuery(query)
                    .GetValueOrDefault("returnUrl").ToString();
        }

        return null;
    }

    /// <summary>
    /// True for paths under /Account, the login and registration pages.
    /// </summary>
    public static bool IsAccountPath(string path)
    {
        return path != null
            && (path.Equals("/Account", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Account/", StringComparison.OrdinalIgnoreCase));
    }

    private static (string Path, string Query) SplitPathAndQuery(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return (absolute.AbsolutePath, absolute.Query);
        }

        var queryStart = url.IndexOf('?');
        return queryStart < 0
            ? (url, string.Empty)
            : (url[..queryStart], url[queryStart..]);
    }
}
