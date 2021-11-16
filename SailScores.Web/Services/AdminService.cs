using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class AdminService : IAdminService
{
    private readonly Core.Services.IClubService _coreClubService;
    private readonly Core.Services.IScoringService _coreScoringService;
    private readonly Core.Services.IRaceService _coreRaceService;
    private readonly IWeatherService _weatherService;
    private readonly IPermissionService _permissionService;
    private readonly IMapper _mapper;

    public AdminService(
        CoreServices.IClubService clubService,
        CoreServices.IScoringService scoringService,
        CoreServices.IRaceService raceService,

        IWeatherService weatherService,
        IPermissionService permissionService,
        IMapper mapper)
    {
        _coreClubService = clubService;
        _coreScoringService = scoringService;
        _coreRaceService = raceService;
        _weatherService = weatherService;
        _permissionService = permissionService;
        _mapper = mapper;
    }


    public async Task<AdminViewModel> GetClubForEdit(string clubInitials)
    {

        var club = await _coreClubService.GetClubForAdmin(clubInitials);

        var vm = _mapper.Map<AdminViewModel>(club);
        vm.ScoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(club.Id, true);
        vm.SpeedUnitOptions = _weatherService.GetSpeedUnitOptions();
        vm.TemperatureUnitOptions = _weatherService.GetTemperatureUnitOptions();
        return vm;

    }

    public async Task<AdminViewModel> GetClub(string clubInitials)
    {
        var club = await _coreClubService.GetClubForAdmin(clubInitials);

        var vm = _mapper.Map<AdminViewModel>(club);
        vm.ScoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(club.Id, true);
        vm.HasCompetitors = vm.BoatClasses.Count != 0 &&
                            (await _coreClubService.HasCompetitorsAsync(club.Id));
        vm.HasRaces = vm.BoatClasses.Count != 0 &&
                      (await _coreRaceService.HasRacesAsync(club.Id));
        vm.Users = await _permissionService.GetUsersAsync(club.Id);

        return vm;
    }

    public async Task UpdateClub(Club clubObject)
    {
        await _coreClubService.UpdateClub(clubObject);
    }
}