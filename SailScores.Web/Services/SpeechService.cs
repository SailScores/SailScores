using Microsoft.Extensions.Configuration;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class SpeechService : ISpeechService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SpeechService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration
            )
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public string GetRegion()
        {
            return _configuration["AzureSpeechRegion"];
        }

        public async Task<string> GetToken()
        {

            //Request url for the speech api.
            string uri = $"https://{GetRegion()}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
            //Generate Speech Synthesis Markup Language (SSML)

            using (var client = _httpClientFactory.CreateClient())
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(uri);
                    request.Headers.Add("Ocp-Apim-Subscription-Key",
                        _configuration["AzureSpeechSubscriptionKey"]);

                    var response = await client.SendAsync(request);
                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return responseString;
                }
            }
        }
    }
}
