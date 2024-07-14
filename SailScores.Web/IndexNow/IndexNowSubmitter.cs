using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using SailScores.Web.Services;
using System.Net.Http;
using System.Net.Http.Json;

namespace SailScores.Web.IndexNow;

public class IndexNowSubmitter : IIndexNowSubmitter
{
    private readonly ILogger<IndexNowSubmitter> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppSettingsService _settingsService;

    public IndexNowSubmitter(
        ILogger<IndexNowSubmitter> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        AppSettingsService settingsService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _settingsService = settingsService;
    }

    public async Task SubmitUrls(
        IList<string> urlList)
    {
        var settings = _settingsService.GetIndexNowConfig(
            _httpContextAccessor.HttpContext.Request);

        var fullUrls = ConvertToFullUrls(urlList);
        var submission = new Submission
        {
            Host = settings.Host,
            Key = settings.Token,
            KeyLocation = settings.KeyLocation,
            UrlList = fullUrls
        };

        if (settings.KeyLocation.Contains("localhost"))
        {
            _logger.LogWarning("IndexNow key location is localhost, skipping submission");
            return;
        }
        if(fullUrls.Count == 0)
        {
            _logger.LogWarning("No URLs to submit to IndexNow");
            return;
        }
        await SubmitToIndexNowAsync(settings.SubmissionUrl, submission);
    }

    private List<string> ConvertToFullUrls(
        IList<string> urlList)
    {
        var context = _httpContextAccessor.HttpContext;
        var baseUrl = _settingsService.GetPreferredBase(context.Request);
        var returnList = new List<string>();
        foreach (var url in urlList)
        {
            if (!url.StartsWith("/"))
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    returnList.Add(uri.AbsoluteUri);
                } else
                {
                    _logger.LogWarning($"Invalid URL: {url}");
                }
            } else
            {
                returnList.Add($"{baseUrl}{url}");
            }
        }
        return returnList;
    }

    private async Task SubmitToIndexNowAsync(
        string submissionUrl,
        Submission submission)
    {
        try {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(submissionUrl, submission);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("IndexNow submission successful");
            }
            else
            {
                _logger.LogError("IndexNow submission failed with status code {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting to IndexNow");
        }
    }
}
