﻿using Microsoft.AspNetCore.Localization;
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
        IMapper mapper)
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
    }


    public async Task<AdminViewModel> GetClubForEdit(string clubInitials)
    {

        var club = await _coreClubService.GetClubForAdmin(clubInitials);

        var vm = _mapper.Map<AdminViewModel>(club);
        vm.ScoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(club.Id, true);
        vm.SpeedUnitOptions = _weatherService.GetSpeedUnitOptions();
        vm.TemperatureUnitOptions = _weatherService.GetTemperatureUnitOptions();
        vm.LocaleOptions = GetLocaleLongNames();
        vm.Locale = GetLocaleLongName(club.Locale);
        return vm;

    }

    private string GetLocaleLongName(string locale)
    {
        if(String.IsNullOrWhiteSpace(locale))
        {
            return _localizerService.SupportedLocalizations[_localizerService.DefaultLocalization];
        }
        var returnValue = _localizerService.SupportedLocalizations[locale];

        return returnValue ?? _localizerService.SupportedLocalizations[_localizerService.DefaultLocalization]; 

    }

    private IList<string> GetLocaleLongNames()
    {
        return _localizerService.SupportedLocalizations.Values.ToList();
    }

    public string GetLocaleShortName(string locale)
    {
        var returnValue = _localizerService.SupportedLocalizations.FirstOrDefault( l => l.Value == locale).Key;

        return returnValue ?? _localizerService.SupportedLocalizations[_localizerService.DefaultLocalization];

    }

    public async Task<AdminViewModel> GetClub(string clubInitials)
    {
        var club = await _coreClubService.GetClubForAdmin(clubInitials);

        var vm = _mapper.Map<AdminViewModel>(club);

        foreach (var boatClass in vm.BoatClasses ?? new List<BoatClassDeleteViewModel>()) {
            var deletableInfo = await _coreBoatClassService.GetDeletableInfo(boatClass.Id);
            boatClass.IsDeletable = deletableInfo.IsDeletable;
            boatClass.PreventDeleteReason = deletableInfo.Reason;
        }

        var fleetDeleteInfo = await _coreFleetService.GetDeletableInfo(club.Id);
        var fleetRegattaInfo = await _coreFleetService.GetClubRegattaFleets(club.Id);
        foreach (var fleet in vm.Fleets)
        {
            var delInfo = fleetDeleteInfo.FirstOrDefault(fdi => fdi.Id == fleet.Id);
            fleet.IsDeletable = delInfo.IsDeletable;
            fleet.PreventDeleteReason = delInfo.IsDeletable ?
                String.Empty : "Fleet has races assigned.";
            fleet.IsRegattaFleet = fleetRegattaInfo.Any(f => f.Key == fleet.Id);
        }

        var seasonDeleteInfo = await _coreSeasonService.GetDeletableInfo(club.Id);
        foreach (var season in vm.Seasons)
        {
            var delInfo = seasonDeleteInfo.FirstOrDefault(fdi => fdi.Id == season.Id);
            season.IsDeletable = delInfo.IsDeletable;
            season.PreventDeleteReason = delInfo.IsDeletable ?
                String.Empty : "Season has series assigned.";
        }
        var scoringSysDeleteInfo = await _coreScoringService.GetDeletableInfo(club.Id);
        foreach (var scoringSystem in vm.ScoringSystems)
        {
            var delInfo = scoringSysDeleteInfo.FirstOrDefault(fdi => fdi.Id == scoringSystem.Id);
            scoringSystem.IsDeletable = delInfo.IsDeletable;
            scoringSystem.PreventDeleteReason = delInfo.IsDeletable ?
                String.Empty : "Scoring System is in use.";
        }

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
        await _localizerService.UpdateCulture(clubObject.Initials, clubObject.Locale);
        await _coreClubService.UpdateClub(clubObject);
    }
}