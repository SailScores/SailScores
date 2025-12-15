using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using SailScores.Core.Model;
using SailScores.Database.Migrations;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Resources;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class AdminService : IAdminService
{
    private readonly CoreServices.IClubService _coreClubService;
    private readonly CoreServices.IScoringService _coreScoringService;
    private readonly CoreServices.IRaceService _coreRaceService;
    private readonly CoreServices.IBoatClassService _coreBoatClassService;
    private readonly CoreServices.IFleetService _coreFleetService;
    private readonly CoreServices.ISeasonService _coreSeasonService;
    private readonly IWeatherService _weatherService;
    private readonly IPermissionService _permissionService;
    private readonly ILocalizerService _localizerService;
    private readonly IMapper _mapper;
    private readonly IHostEnvironment _env;

    public AdminService(
        CoreServices.IClubService clubService,
        CoreServices.IScoringService scoringService,
        CoreServices.IRaceService raceService,
        CoreServices.IBoatClassService boatClassService,
        CoreServices.IFleetService fleetService,
        CoreServices.ISeasonService seasonService,
        IWeatherService weatherService,
        IPermissionService permissionService,
        ILocalizerService localizerService,
        IMapper mapper,
        IHostEnvironment env)
    {
        _coreClubService = clubService;
        _coreScoringService = scoringService;
        _coreRaceService = raceService;
        _coreBoatClassService = boatClassService;
        _coreFleetService = fleetService;
        _coreSeasonService = seasonService;
        _weatherService = weatherService;
        _permissionService = permissionService;
        _localizerService = localizerService;
        _mapper = mapper;
        _env = env;
    }

    public async Task<AdminViewModel> GetClubForEdit(string clubInitials)
    {
        var club = await _coreClubService.GetClubForAdmin(clubInitials);

        var vm = _mapper.Map<AdminViewModel>(club);
        vm.ScoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(club.Id, false);
        vm.SpeedUnitOptions = _weatherService.GetSpeedUnitOptions();
        vm.TemperatureUnitOptions = _weatherService.GetTemperatureUnitOptions();
        vm.LocaleOptions = GetLocaleLongNames();
        vm.Locale = _localizerService.GetLocaleLongName(club.Locale);
        return vm;
    }

    private IList<string> GetLocaleLongNames()
    {
        var locales = LocalizerService.GetSupportedLocalisations(_env.IsDevelopment());
        return locales.Values.ToList(); ;
    }

    public async Task<AdminViewModel> GetClub(string clubInitials)
    {
        var club = await _coreClubService.GetClubForAdmin(clubInitials);
        var vm = _mapper.Map<AdminViewModel>(club);

        foreach (var boatClass in vm.BoatClasses ?? new List<BoatClassDeleteViewModel>())
        {
            var deletableInfo = await _coreBoatClassService.GetDeletableInfo(boatClass.Id);
            boatClass.IsDeletable = deletableInfo.IsDeletable;
            boatClass.PreventDeleteReason = deletableInfo.IsDeletable ? string.Empty : "Fleet has races assigned.";
        }

        var fleetDeleteInfo = await _coreFleetService.GetDeletableInfo(club.Id);
        var fleetRegattaInfo = await _coreFleetService.GetClubRegattaFleets(club.Id);
        foreach (var fleet in vm.Fleets)
        {
            var delInfo = fleetDeleteInfo.FirstOrDefault(fdi => fdi.Id == fleet.Id);
            fleet.IsDeletable = delInfo.IsDeletable;
            fleet.PreventDeleteReason = delInfo.IsDeletable ? string.Empty : "Fleet has races assigned.";
            fleet.IsRegattaFleet = fleetRegattaInfo.Any(f => f.Key == fleet.Id);
        }

        var seasonDeleteInfo = await _coreSeasonService.GetDeletableInfo(club.Id);
        foreach (var season in vm.Seasons)
        {
            var delInfo = seasonDeleteInfo.FirstOrDefault(fdi => fdi.Id == season.Id);
            season.IsDeletable = delInfo.IsDeletable;
            season.PreventDeleteReason = delInfo.IsDeletable ? string.Empty : "Season has series assigned.";
        }

        var scoringSysDeleteInfo = await _coreScoringService.GetDeletableInfo(club.Id);
        foreach (var scoringSystem in vm.ScoringSystems)
        {
            var delInfo = scoringSysDeleteInfo.FirstOrDefault(fdi => fdi.Id == scoringSystem.Id);
            scoringSystem.IsDeletable = delInfo.IsDeletable;
            scoringSystem.PreventDeleteReason = delInfo.IsDeletable ? string.Empty : "Scoring System is in use.";
        }

        vm.ScoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(club.Id, false);

        vm.HasCompetitors = vm.BoatClasses.Count != 0 &&
                            (await _coreClubService.HasCompetitorsAsync(club.Id));
        vm.HasRaces = vm.BoatClasses.Count != 0 &&
                      (await _coreRaceService.HasRacesAsync(club.Id));
        vm.Users = await _permissionService.GetUsersAsync(club.Id);

        return vm;
    }

    public async Task UpdateClub(Club clubObject)
    {
        // Map the posted long name to a proper culture code
        var shortLocale = _localizerService.GetLocaleShortName(clubObject.Locale);
        clubObject.Locale = shortLocale;

        await _localizerService.UpdateCulture(clubObject.Initials, shortLocale);
        await _coreClubService.UpdateClub(clubObject);
    }

    public async Task ProcessLogoFile(AdminEditViewModel model)
    {
        if (model.LogoFile != null)
        {
            using (var memoryStream = new MemoryStream())
            {
                await model.LogoFile.CopyToAsync(memoryStream);

                // Upload the file if less than 2 MB
                if (memoryStream.Length < 2097152)
                {
                    var file = new Database.Entities.File
                    {
                        Id = Guid.NewGuid(),
                        FileContents = memoryStream.ToArray(),
                        Created = DateTime.UtcNow
                    };

                    await _coreClubService.SaveFileAsync(file);
                    model.LogoFileId = file.Id;
                }
                else
                {
                    throw new ArgumentException("File is too large. Maximum size is 2 MB.");
                }
            }
        }
    }

    public async Task<FileStreamResult> GetLogoAsync(Guid id)
    {
        var file = await _coreClubService.GetFileAsync(id);
        if (file == null)
        {
            return null;
        }
        var stream = new MemoryStream();
        stream.Write(file.FileContents, 0, file.FileContents.Length);
        stream.Position = 0;
        return new FileStreamResult(stream, "image/png");
    }
}
