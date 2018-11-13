using Newtonsoft.Json;
using Sailscores.Client.Uwp.Services.SettingsServices;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using SailScores.Core.Model.Dto;

namespace Sailscores.Client.Uwp.Services
{
    public class SailscoresApiClient
    {
        private SettingsService _settings;
        private string _token;

        public SailscoresApiClient(SettingsService settings)
        {
            this._settings = settings;
        }

        public async Task<T> GetAsync<T>(string urlExtension)
        {
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            if(_token != null)
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);
            }

            Uri requestUri = new Uri(new Uri(_settings.ServerUrl), urlExtension);

            //Send the GET request asynchronously and retrieve the response as a string.
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            try
            {
                //Send the GET request
                httpResponse = await httpClient.GetAsync(requestUri);
                if(httpResponse.StatusCode == Windows.Web.Http.HttpStatusCode.Unauthorized)
                {
                    await AuthenticateAsync();
                    return await GetAsync<T>(urlExtension);
                }
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                var returnObj = JsonConvert.DeserializeObject<T>(httpResponseBody);
                return returnObj;
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                throw;
            }
        }

        private async Task AuthenticateAsync()
        {
            if(_settings.UserCredentials == null
                || String.IsNullOrWhiteSpace(_settings.UserCredentials.UserName)
                || String.IsNullOrWhiteSpace(_settings.UserCredentials.Password))
            {
                throw new UnauthorizedAccessException("Could not login. Please provide credentials.");
            }
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            
            Uri requestUri = new Uri(new Uri(_settings.ServerUrl), "/account/jwttoken");

            //Send the GET request asynchronously and retrieve the response as a string.
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";
  
            var content = JsonConvert.SerializeObject( new {
                email = _settings.UserCredentials.UserName ,
                password= _settings.UserCredentials.Password 
            });

            try
            {
                //Send the GET request
                httpResponse = await httpClient.PostAsync(requestUri,
                    new HttpStringContent(
                        content,
                        Windows.Storage.Streams.UnicodeEncoding.Utf8,
                        "application/json"));
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                _token = httpResponseBody;
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                throw;
            }
        }

        public async Task<List<Club>> GetClubsAsync()
        {
            return await GetAsync<List<Club>>("/api/club");
        }

        public async Task<ClubDto> GetClubAsync(Guid clubId)
        {
            return await GetAsync<ClubDto>($"/api/club/{clubId}");
        }

        public async Task<List<Competitor>> GetCompetitorsAsync(Guid clubId)
        {
            return await GetAsync<List<Competitor>>($"/api/competitor/&clubId={clubId}");
        }

        public async Task<List<Series>> GetAllSeriesAsync(Guid clubId)
        {
            return await GetAsync<List<Series>>($"/api/series/{clubId}");
        }

        public async Task<Series> GetOneSeriesAsync(Guid clubId)
        {
            return await GetAsync<Series>($"/api/series/{clubId}");
        }

        //TODO: What filter do I need on this? (Don't want to load all for club.)
        public async Task<List<Race>> GetRacesAsync(Guid clubId)
        {
            return await GetAsync<List<Race>>($"/api/race/{clubId}");
        }

    }
}
