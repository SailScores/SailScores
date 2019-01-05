using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SailScores.Api.Dtos;
using SailScores.Api.Services;
using SailScores.Client.Uwp.Services.SettingsServices;
using SailScores.Core.Model;

namespace SailScores.Client.Uwp.Services
{
    public class SailScoresServerService
    {
        private SettingsService _settings;
        private SailScoresApiClient _apiClient;
        private List<Club> _clubs;

        public SailScoresServerService(
            SettingsService settings,
            SailScoresApiClient apiClient
            )
        {
            this._settings = settings;
            this._apiClient = apiClient;
        }

        public static SailScoresServerService GetInstance(SettingsService settings)
        {
            return new SailScoresServerService(settings,
                new SailScoresApiClient(settings));
        }


        public async Task<List<ClubDto>> GetClubsAsync()
        {
            return await _apiClient.GetClubsAsync();
        }

        public async Task LoadCurrentClubAsync()
        {
            if (_settings.ClubId.HasValue)
            {
                var club = await _apiClient.GetClubAsync(_settings.ClubId.Value);
            }
        }
    }
}
