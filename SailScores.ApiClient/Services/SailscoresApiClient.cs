using Newtonsoft.Json;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SailScores.Core.Model.Dto;
using System.Net.Http;
using System.Net;

namespace SailScores.ApiClient.Services
{
    public class SailScoresApiClient
    {
        private ISettings _settings;
        private string _token;

        public SailScoresApiClient(ISettings settings)
        {
            this._settings = settings;
        }

        public async Task<T> GetAsync<T>(string urlExtension)
        {
            using (var httpClient = new HttpClient())
            {
                if (_token != null)
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);
                }

                Uri requestUri = new Uri(new Uri(_settings.ServerUrl), urlExtension);

                //Send the GET request asynchronously and retrieve the response as a string.
                var httpResponse = new HttpResponseMessage();
                string httpResponseBody = "";

                try
                {
                    //Send the GET request
                    httpResponse = await httpClient.GetAsync(requestUri);
                    if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
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
        }

        private async Task AuthenticateAsync()
        {
            if( String.IsNullOrWhiteSpace(_settings.UserName)
                || String.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new UnauthorizedAccessException("Could not login. Please provide credentials.");
            }
            using (var httpClient = new HttpClient())
            {
                Uri requestUri = new Uri(new Uri(_settings.ServerUrl), "/account/jwttoken");

                //Send the GET request asynchronously and retrieve the response as a string.
                var httpResponse = new HttpResponseMessage();
                string httpResponseBody = "";

                var content = JsonConvert.SerializeObject(new
                {
                    email = _settings.UserName,
                    password = _settings.Password
                });

                try
                {
                    //Send the GET request
                    httpResponse = await httpClient.PostAsync(requestUri,
                        new StringContent(
                            content,
                            UnicodeEncoding.UTF8,
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
