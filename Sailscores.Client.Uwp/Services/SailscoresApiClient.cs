using Newtonsoft.Json;
using Sailscores.Client.Uwp.Services.SettingsServices;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sailscores.Client.Uwp.Services
{
    public class SailscoresApiClient
    {
        private SettingsService _settings;

        public SailscoresApiClient(SettingsService settings)
        {
            this._settings = settings;
        }

        public async Task<T> GetAsync<T>(string urlExtension)
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

            Uri requestUri = new Uri(new Uri(_settings.ServerUrl), urlExtension);

            //Send the GET request asynchronously and retrieve the response as a string.
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            try
            {
                //Send the GET request
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                var clubs = JsonConvert.DeserializeObject<T>(httpResponseBody);
                return clubs;
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                throw;
            }
        }

        public async Task<List<Club>> GetClubsAsync()
        {
            return await GetAsync<List<Club>>("Club");
        }
    }
}
