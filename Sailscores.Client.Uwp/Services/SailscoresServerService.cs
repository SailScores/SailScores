using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sailscores.Client.Uwp.Services.SettingsServices;
using SailScores.Core.Model;

namespace Sailscores.Client.Uwp.Services
{
    public class SailscoresServerService
    {
        SettingsServices.SettingsService _settings;
        private SettingsService settings;

        private List<Club> _clubs;

        public SailscoresServerService(SettingsService settings)
        {
            this.settings = settings;
        }

        public static SailscoresServerService GetInstance(SettingsService settings)
        {
            return new SailscoresServerService(settings);
        }

        public List<Club> GetClubs()
        {
            if (_clubs == null)
            {
                _clubs = new List<Club>
                {
                    new Club{
                    Id = Guid.Parse("{43D00C5B-4A79-4903-87CA-379DEB90E4D7}"),
                    Name = "Test Club 1",
                    Initials = "TCYC"
                    },
                    new Club{
                    Id = Guid.Parse("{A1A7015D-8DA4-4CE5-9C68-3A978F980CFC}"),
                    Name = "Test Club 2",
                    Initials = "TEST"
                    },
                };
            }
            return _clubs;
        }


        public async Task<List<Club>> GetClubsAsync()
        {
            await Task.Delay(5000);
            return GetClubs();
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

            Uri requestUri = new Uri(new Uri(_settings.ServerUrl), "Club");

            //Send the GET request asynchronously and retrieve the response as a string.
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            try
            {
                //Send the GET request
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                var clubs = JsonConvert.DeserializeObject<List<Club>>(httpResponseBody);
                return clubs;
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                throw;
            }
        }

    }
}
