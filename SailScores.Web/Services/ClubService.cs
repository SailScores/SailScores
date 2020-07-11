using SailScores.Core.Model;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class ClubService : IClubService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly Core.Services.ISeasonService _coreSeasonService;
        private readonly Core.Services.IRaceService _coreRaceService;
        private readonly Core.Services.ISeriesService _coreSeriesService;
        private readonly Core.Services.IRegattaService _coreRegattaService;


        public ClubService(
            Core.Services.IClubService clubService,
            Core.Services.ISeasonService seasonService,
            Core.Services.IRaceService raceService,
            Core.Services.ISeriesService seriesService,
            Core.Services.IRegattaService regattaService)
        {
            _coreClubService = clubService;
            _coreSeasonService = seasonService;
            _coreRaceService = raceService;
            _coreSeriesService = seriesService;
            _coreRegattaService = regattaService;
        }

        public async Task<Club> GetClubForClubHome(string clubInitials)
        {
            var clubId = await _coreClubService.GetClubId(clubInitials);
            var club = await _coreClubService.GetMinimalClub(clubId);
            club.Seasons = await _coreSeasonService.GetSeasons(clubId);
            club.Races = await _coreRaceService.GetRacesAsync(clubId);
            club.Series = await _coreSeriesService.GetAllSeriesAsync(clubId, null, false);
            club.Regattas = await _coreRegattaService.GetAllRegattasAsync(clubId);
            return club;
        }
    }
}
