using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SailScores.Core.Services
{
    public class IndexNowService : IIndexNowService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IndexNowService> _logger;

        public IndexNowService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<IndexNowService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task NotifySeriesUpdate(
            Guid clubId,
            string clubInitials,
            string seasonUrlName,
            string seriesUrlName,
            bool isClubHidden)
        {
            // Skip IndexNow for hidden clubs
            if (isClubHidden)
            {
                _logger.LogDebug("Skipping IndexNow notification for hidden club {ClubInitials}", clubInitials);
                return;
            }

            try
            {
                var apiKey = _configuration["IndexNow:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("IndexNow API key not configured. Skipping notification.");
                    return;
                }

                var preferredHost = _configuration["PreferredHost"];
                if (string.IsNullOrEmpty(preferredHost))
                {
                    _logger.LogWarning("PreferredHost not configured. Skipping IndexNow notification.");
                    return;
                }

                var baseUrl = $"https://{preferredHost}";
                var seriesUrl = $"{baseUrl}/{clubInitials}/series/{seasonUrlName}/{seriesUrlName}";

                var payload = new
                {
                    host = new Uri(baseUrl).Host,
                    key = apiKey,
                    keyLocation = $"{baseUrl}/{apiKey}.txt",
                    urlList = new[] { seriesUrl }
                };

                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync(
                    "https://api.indexnow.org/indexnow",
                    payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("IndexNow notification sent for series: {SeriesUrl}", seriesUrl);
                }
                else
                {
                    _logger.LogWarning("IndexNow notification failed with status {StatusCode} for {SeriesUrl}",
                        response.StatusCode, seriesUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending IndexNow notification for series {SeriesUrlName}", seriesUrlName);
            }
        }
    }
}
