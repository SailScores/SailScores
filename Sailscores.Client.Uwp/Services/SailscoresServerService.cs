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
        private SettingsService _settings;
        private SailscoresApiClient _apiClient;
        private List<Club> _clubs;

        public SailscoresServerService(
            SettingsService settings,
            SailscoresApiClient apiClient
            )
        {
            this._settings = settings;
            this._apiClient = apiClient;
        }

        public static SailscoresServerService GetInstance(SettingsService settings)
        {
            return new SailscoresServerService(settings,
                new SailscoresApiClient(settings));
        }


        public async Task<List<Club>> GetClubsAsync()
        {
            return await _apiClient.GetClubsAsync();
        }

    }
}
