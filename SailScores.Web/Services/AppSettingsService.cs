using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace SailScores.Web.Services;

public class AppSettingsService
{
    private readonly IConfiguration _config;

    public AppSettingsService(
        IConfiguration config)
    {
        _config = config;
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

    public IndexNow.Configuration GetIndexNowConfig(HttpRequest request)
    {
        var baseUrl = GetPreferredBase(request);
        var token = _config["IndexNow:Token"];
        var keyLocation = $"{baseUrl}/{token}.txt";
        string host;
        var preferredhost = _config["PreferredHost"];
        if (!String.IsNullOrWhiteSpace(preferredhost))
        {
            host = preferredhost;
        } else
        {
            host = request.Host.ToUriComponent();
        }

        return new IndexNow.Configuration
        {
            Host = host,
            Token = token,
            KeyLocation = keyLocation,
            SubmissionUrl = _config["IndexNow:SubmissionUrl"]
        };
    }
}