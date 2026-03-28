using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class TurnstileService : ITurnstileService
{
    private const string VerifyEndpoint =
        "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TurnstileService> _logger;
    private readonly string _siteKey;
    private readonly string _secretKey;

    public TurnstileService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TurnstileService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _siteKey = configuration["Turnstile:SiteKey"];
        _secretKey = configuration["Turnstile:SecretKey"];
    }

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(_siteKey) && !string.IsNullOrWhiteSpace(_secretKey);

    public string SiteKey => _siteKey;

    public async Task<bool> VerifyAsync(
        string token,
        IPAddress remoteIpAddress,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        using var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, VerifyEndpoint)
        {
            Content = new FormUrlEncodedContent(BuildPayload(token, remoteIpAddress))
        };

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Turnstile verification failed with status code {StatusCode}.",
                    response.StatusCode);
                return false;
            }

            await using var responseStream = await response.Content
                .ReadAsStreamAsync(cancellationToken);
            var verification = await JsonSerializer.DeserializeAsync<TurnstileVerifyResponse>(
                responseStream,
                cancellationToken: cancellationToken);
            if (verification?.Success == true)
            {
                return true;
            }

            if (verification?.ErrorCodes?.Count > 0)
            {
                _logger.LogWarning(
                    "Turnstile verification failed with error codes: {ErrorCodes}",
                    string.Join(", ", verification.ErrorCodes));
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while verifying Turnstile captcha.");
            return false;
        }
    }

    private IEnumerable<KeyValuePair<string, string>> BuildPayload(
        string token,
        IPAddress remoteIpAddress)
    {
        yield return new KeyValuePair<string, string>("secret", _secretKey);
        yield return new KeyValuePair<string, string>("response", token);

        if (remoteIpAddress != null)
        {
            yield return new KeyValuePair<string, string>(
                "remoteip",
                remoteIpAddress.ToString());
        }
    }

    private sealed class TurnstileVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public List<string> ErrorCodes { get; set; }
    }
}
