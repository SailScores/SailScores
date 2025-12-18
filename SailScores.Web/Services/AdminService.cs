using System.IO;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IClubService _webClubService;
    private readonly IMapper _mapper;
    private readonly IHostEnvironment _env;

    public AdminService(
        CoreServices.IClubService coreClubService,
        CoreServices.IScoringService scoringService,
        CoreServices.IRaceService raceService,
        CoreServices.IBoatClassService boatClassService,
        CoreServices.IFleetService fleetService,
        CoreServices.ISeasonService seasonService,
        IWeatherService weatherService,
        IPermissionService permissionService,
        ILocalizerService localizerService,
        IClubService clubService,
        IMapper mapper,
        IHostEnvironment env)
    {
        _coreClubService = coreClubService;
        _coreScoringService = scoringService;
        _coreRaceService = raceService;
        _coreBoatClassService = boatClassService;
        _coreFleetService = fleetService;
        _coreSeasonService = seasonService;
        _weatherService = weatherService;
        _permissionService = permissionService;
        _localizerService = localizerService;
        _webClubService = clubService;
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
        // invalidate the minimal club cache:
        _webClubService.InvalidateClubCache(clubObject.Initials);

    }

    private const int BytesPerMegabyte = 1048576;
    private const int MaxLogoFileSizeBytes = 2 * BytesPerMegabyte; // 2 MB
    // Note: SVG not supported due to XSS security concerns (SVG can contain JavaScript)
    private static readonly string[] AllowedImageContentTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp" };

    public async Task ProcessLogoFile(AdminEditViewModel model)
    {
        if (model.LogoFile != null)
        {
            // Validate content type
            if (!AllowedImageContentTypes.Contains(model.LogoFile.ContentType?.ToLowerInvariant()))
            {
                throw new ArgumentException($"Invalid file type. Allowed types: PNG, JPG, GIF, WebP. Received: {model.LogoFile.ContentType}");
            }

            using (var memoryStream = new MemoryStream())
            {
                await model.LogoFile.CopyToAsync(memoryStream);
                var fileContents = memoryStream.ToArray();

                // Validate file size
                if (fileContents.Length > MaxLogoFileSizeBytes)
                {
                    throw new ArgumentException($"File is too large. Maximum size is {MaxLogoFileSizeBytes / BytesPerMegabyte} MB.");
                }

                // Validate file signature matches content type
                if (!ValidateFileSignature(fileContents, model.LogoFile.ContentType))
                {
                    throw new ArgumentException("File content does not match the declared content type. Possible file type spoofing detected.");
                }

                var file = new Database.Entities.File
                {
                    Id = Guid.NewGuid(),
                    FileContents = fileContents,
                    Created = DateTime.UtcNow
                };

                await _coreClubService.SaveFileAsync(file);
                model.LogoFileId = file.Id;
            }
        }
    }

    private bool ValidateFileSignature(byte[] fileContents, string contentType)
    {
        if (fileContents == null || fileContents.Length < 4)
        {
            return false;
        }

        var detectedType = DetermineContentType(fileContents);
        var normalizedContentType = contentType?.ToLowerInvariant();

        // Allow jpeg/jpg mismatch
        if ((normalizedContentType == "image/jpeg" || normalizedContentType == "image/jpg") && detectedType == "image/jpeg")
        {
            return true;
        }

        return detectedType == normalizedContentType;
    }

    public async Task<FileStreamResult> GetLogoAsync(Guid id)
    {
        var file = await _coreClubService.GetFileAsync(id);
        if (file == null)
        {
            return null;
        }
        
        // Create stream efficiently without copying
        // FileStreamResult will dispose the stream when done
        var stream = new MemoryStream(file.FileContents, writable: false);
        
        // Determine content type from file content or default to PNG
        var contentType = DetermineContentType(file.FileContents);
        return new FileStreamResult(stream, contentType);
    }

    private string DetermineContentType(byte[] fileContents)
    {
        if (fileContents == null || fileContents.Length < 4)
        {
            return "image/png"; // default
        }

        // Check file signatures (magic numbers) for supported formats only
        if (fileContents[0] == 0x89 && fileContents[1] == 0x50 && fileContents[2] == 0x4E && fileContents[3] == 0x47)
            return "image/png";
        if (fileContents[0] == 0xFF && fileContents[1] == 0xD8 && fileContents[2] == 0xFF)
            return "image/jpeg";
        if (fileContents[0] == 0x47 && fileContents[1] == 0x49 && fileContents[2] == 0x46)
            return "image/gif";
        // WebP: RIFF....WEBP (check for "RIFF" at start and "WEBP" at bytes 8-11)
        if (fileContents.Length >= 12 && 
            fileContents[0] == 0x52 && fileContents[1] == 0x49 && fileContents[2] == 0x46 && fileContents[3] == 0x46 &&
            fileContents[8] == 0x57 && fileContents[9] == 0x45 && fileContents[10] == 0x42 && fileContents[11] == 0x50)
            return "image/webp";

        return "image/png"; // default fallback
    }
}
