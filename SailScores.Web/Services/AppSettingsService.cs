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
