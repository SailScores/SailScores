using AutoMapper;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class AdminService : IAdminService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly Core.Services.ISeasonService _coreSeasonService;
        private readonly Core.Services.IRaceService _coreRaceService;
        private readonly Core.Services.ISeriesService _coreSeriesService;
        private readonly Core.Services.IRegattaService _coreRegattaService;
        private readonly Core.Services.IScoringService _coreScoringService;
        private readonly IWeatherService _weatherService;
        private readonly IMapper _mapper;

        public AdminService(
            Core.Services.IClubService clubService,
            Core.Services.ISeasonService seasonService,
            Core.Services.IRaceService raceService,
            Core.Services.ISeriesService seriesService,
            Core.Services.IRegattaService regattaService,
            Core.Services.IScoringService scoringService,
            Services.IWeatherService weatherService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreSeasonService = seasonService;
            _coreRaceService = raceService;
            _coreSeriesService = seriesService;
            _coreRegattaService = regattaService;
            _coreScoringService = scoringService;
            _weatherService = weatherService;
            _mapper = mapper;
        }


        public async Task<AdminViewModel> GetClubForEdit(string clubInitials)
        {

            var club = await _coreClubService.GetFullClubExceptScores(clubInitials);

            var vm = _mapper.Map<AdminViewModel>(club);
            vm.ScoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(club.Id, true);
            vm.SpeedUnitOptions = _weatherService.GetSpeedUnitOptions();
            vm.TemperatureUnitOptions = _weatherService.GetTemperatureUnitOptions();
            return vm;

        }

        public async Task<AdminViewModel> GetClub(string clubInitials)
        {
            var club = await _coreClubService.GetFullClubExceptScores(clubInitials);

            var vm = _mapper.Map<AdminViewModel>(club);
            vm.ScoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(club.Id, true);

            return vm;
        }

        public async Task UpdateClub(Club clubObject)
        {
            await _coreClubService.UpdateClub(clubObject);
        }

        //public async Task<Club> GetFullClub(string clubInitials)
        //{
        //    return await _coreClubService.GetFullClub(clubInitials);
        //}

        //public async Task<Club> GetClubForClubHome(string clubInitials)
        //{
        //    var clubId = await _coreClubService.GetClubId(clubInitials);
        //    var club = await _coreClubService.GetMinimalClub(clubId);
        //    club.Seasons = await _coreSeasonService.GetSeasons(clubId);
        //    club.Races = await _coreRaceService.GetRacesAsync(clubId);
        //    club.Series = await _coreSeriesService.GetAllSeriesAsync(clubId, null, false);
        //    club.Regattas = await _coreRegattaService.GetAllRegattasAsync(clubId);
        //    return club;
        //}

    }
}
