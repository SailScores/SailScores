using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SailScores.Api.Dtos;
using System.Net.Http;
using System.Net;

namespace SailScores.Api.Services
{
    public class SailScoresApiClient : ISailScoresApiClient
    {
        private readonly ISettings _settings;
        private string _token;

        public SailScoresApiClient(ISettings settings)
        {
            this._settings = settings;
        }

        public async Task<T> GetAsync<T>(string urlExtension, Guid clubId)
        {
            return await GetAsync<T>($"{urlExtension}?clubId={clubId}");
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
                var httpResponse = await httpClient.GetAsync(requestUri);
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await AuthenticateAsync();
                    return await GetAsync<T>(urlExtension);
                }
                httpResponse.EnsureSuccessStatusCode();
                var httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                var returnObj = JsonConvert.DeserializeObject<T>(httpResponseBody);
                return returnObj;
            }
        }

        public async Task<Guid> PostAsync<T>(string urlExtension, T item, int retryCount = 0)
        {
            using (var httpClient = new HttpClient())
            {
                if (_token != null)
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _token);
                }

                Uri requestUri = new Uri(new Uri(_settings.ServerUrl), urlExtension);

                var json = JsonConvert.SerializeObject(item);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                //Send the request
                var httpResponse = await httpClient.PostAsync(requestUri, content);
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (retryCount < 2)
                    {
                        await AuthenticateAsync();
                        return await PostAsync<T>(urlExtension, item, ++retryCount);
                    }
                    else
                    {
                        throw new UnauthorizedAccessException("Unauthorized for Web API. Check Credentials.");
                    }
                }
                httpResponse.EnsureSuccessStatusCode();
                var httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                var returnId = JsonConvert.DeserializeObject<Guid>(httpResponseBody);
                return returnId;

            }
        }

        private async Task AuthenticateAsync()
        {
            if (String.IsNullOrWhiteSpace(_settings.UserName)
                || String.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new UnauthorizedAccessException("Could not login. Please provide credentials.");
            }
            using (var httpClient = new HttpClient())
            {
                Uri requestUri = new Uri(new Uri(_settings.ServerUrl), "/account/jwttoken");

                var content = JsonConvert.SerializeObject(new
                {
                    email = _settings.UserName,
                    password = _settings.Password
                });

                var httpResponse = await httpClient.PostAsync(requestUri,
                    new StringContent(
                        content,
                        UnicodeEncoding.UTF8,
                        "application/json"));
                httpResponse.EnsureSuccessStatusCode();
                var httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                _token = httpResponseBody;

            }
        }

        public async Task<List<ClubDto>> GetClubsAsync()
        {
            return await GetAsync<List<ClubDto>>("/api/clubs");
        }

        public async Task<ClubDto> GetClubAsync(Guid clubId)
        {
            return await GetAsync<ClubDto>($"/api/clubs/{clubId}");
        }

        public async Task<List<CompetitorDto>> GetCompetitorsAsync(Guid clubId)
        {
            return await GetAsync<List<CompetitorDto>>($"/api/competitors/&clubId={clubId}");
        }

        public async Task<List<SeriesDto>> GetAllSeriesAsync(Guid clubId)
        {
            return await GetAsync<List<SeriesDto>>($"/api/series/{clubId}");
        }

        public async Task<SeriesDto> GetOneSeriesAsync(Guid clubId)
        {
            return await GetAsync<SeriesDto>($"/api/series/");
        }

        public async Task<List<RaceDto>> GetRacesAsync(Guid clubId)
        {
            return await GetAsync<List<RaceDto>>($"/api/races/", clubId);
        }

        public async Task<Guid> SaveClub(ClubDto club)
        {
            return await PostAsync<ClubDto>($"/api/clubs/", club);
        }

        public async Task<List<BoatClassDto>> GetBoatClassesAsync(Guid clubId)
        {
            return await GetAsync<List<BoatClassDto>>($"/api/boatClasses/", clubId);
        }

        public async Task<Guid> SaveBoatClass(BoatClassDto boatClass)
        {
            return await PostAsync<BoatClassDto>($"/api/boatClasses/", boatClass);
        }

        public async Task<List<FleetDto>> GetFleetsAsync(Guid clubId)
        {
            return await GetAsync<List<FleetDto>>($"/api/fleets/", clubId);
        }

        public async Task<Guid> SaveFleet(FleetDto fleet)
        {
            return await PostAsync<FleetDto>($"/api/fleets/", fleet);
        }

        public async Task<Guid> SaveSeries(SeriesDto series)
        {
            return await PostAsync<SeriesDto>($"/api/series/", series);
        }

        public async Task<List<SeasonDto>> GetSeasonsAsync(Guid clubId)
        {

            return await GetAsync<List<SeasonDto>>($"/api/seasons/", clubId);
        }

        public async Task<Guid> SaveSeason(SeasonDto season)
        {
            return await PostAsync<SeasonDto>($"/api/seasons/", season);
        }

        public async Task<List<CompetitorDto>> GetCompetitors(Guid clubId, Guid? fleetId)
        {
            if (fleetId.HasValue)
            {
                return await GetAsync<List<CompetitorDto>>($"/api/competitors/?clubId={clubId}&fleetId={fleetId}");
            }
            else
            {
                return await GetAsync<List<CompetitorDto>>($"/api/competitors/", clubId);
            }
        }

        public async Task<Guid> SaveCompetitor(CompetitorDto competitor)
        {
            return await PostAsync<CompetitorDto>($"/api/competitors/", competitor);
        }

        public async Task<Guid> SaveRace(RaceDto race)
        {
            return await PostAsync<RaceDto>($"/api/races/", race);
        }
    }
}
