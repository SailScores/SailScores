using Microsoft.Extensions.Logging;
using SailScores.Api.Dtos;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using System.ComponentModel;
using IRaceService = SailScores.Web.Services.Interfaces.IRaceService;
using IWeatherService = SailScores.Web.Services.Interfaces.IWeatherService;

namespace SailScores.Web.Services;

public class RaceService : IRaceService
{
    private readonly Core.Services.IClubService _coreClubService;
    private readonly Core.Services.IRaceService _coreRaceService;
    private readonly Core.Services.ISeriesService _coreSeriesService;
    private readonly IScoringService _coreScoringService;
    private readonly Core.Services.IRegattaService _coreRegattaService;
    private readonly Core.Services.ISeasonService _coreSeasonService;
    private readonly CoreServices.ICompetitorService _coreCompetitorService;
    private readonly IWeatherService _weatherService;
    private readonly ISpeechService _speechService;
    private readonly IMapper _mapper;
    private readonly ILogger<RaceService> _logger;

    public RaceService(
        Core.Services.IClubService clubService,
        Core.Services.IRaceService coreRaceService,
        Core.Services.ISeriesService coreSeriesService,
        Core.Services.IScoringService coreScoringService,
        Core.Services.IRegattaService coreRegattaService,
        Core.Services.ISeasonService coreSeasonService,
        Core.Services.ICompetitorService coreCompetitorService,

        IWeatherService weatherService,
        ISpeechService speechService,
        IMapper mapper,
        ILogger<RaceService> logger)
    {
        _coreClubService = clubService;
        _coreRaceService = coreRaceService;
        _coreSeriesService = coreSeriesService;
        _coreScoringService = coreScoringService;
        _coreRegattaService = coreRegattaService;
        _coreSeasonService = coreSeasonService;
        _coreCompetitorService = coreCompetitorService;

        _weatherService = weatherService;
        _speechService = speechService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task Delete(Guid id, string userName)
    {
        await _coreRaceService.Delete(id, userName);
    }

    public async Task<RaceSummaryListViewModel> GetAllRaceSummariesAsync(
        string clubInitials,
        string seasonName,
        bool includeScheduled,
        bool includeAbandoned)
    {
        var club = await _coreClubService.GetMinimalClub(clubInitials);
        var seasons = await _coreSeasonService.GetSeasons(club.Id);
        var selectedSeason = seasons
            .FirstOrDefault(s =>
                s.UrlName == seasonName ||
                s.Name == seasonName);
        if (selectedSeason == null)
        {
            return default;
        }
        var races = (await _coreRaceService.GetFullRacesAsync(
                club.Id,
                seasonName,
                includeScheduled,
                includeAbandoned))
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.Fleet?.Name)
            .ThenBy(r => r.Order);

        // need to get score codes so viewmodel can determine starting boat count.
        var scoreCodes = await _coreScoringService.GetScoreCodesAsync(club.Id);

        var excludedRaces = await _coreRaceService.GetStatsExcludedRaces(club.Id, selectedSeason.Id);
        var racesVm = _mapper.Map<List<RaceSummaryViewModel>>(races);
        foreach (var race in racesVm)
        {
            if (excludedRaces.Contains(race.Id))
            {
                race.ExcludeFromCompStats = true;
            }
            foreach (var score in race.Scores)
            {
                score.ScoreCode = GetScoreCode(score.Code, scoreCodes);
            }
        }

        return new RaceSummaryListViewModel
        {
            Races = racesVm,
            Seasons = seasons,
            CurrentSeason = seasons.FirstOrDefault(s => s.Name == seasonName)
        };
    }

    private static ScoreCode GetScoreCode(string code, IEnumerable<ScoreCode> scoreCodes)
    {
        return scoreCodes.FirstOrDefault(sc => sc.Name == code);
    }

    public async Task<RaceWithOptionsViewModel> GetBlankRaceWithOptions(
        string clubInitials,
        Guid? regattaId,
        Guid? seriesId,
        Guid? fleetId = null)
    {
        RaceWithOptionsViewModel returnRace;
        if (regattaId.HasValue)
        {
            returnRace = await CreateRegattaRaceAsync(clubInitials, regattaId);
        }
        else if (seriesId.HasValue)
        {
            returnRace = await CreateSeriesRaceAsync(clubInitials, seriesId.Value);
        }
        else if (fleetId.HasValue)
        {
            returnRace = await CreateClubRaceAsync(clubInitials);
            if (returnRace.FleetOptions.Any(f => f.Id == fleetId.Value))
            {
                returnRace.FleetId = fleetId.Value;
            }

            returnRace.CompetitorOptions = await _coreCompetitorService.GetCompetitorsAsync(returnRace.ClubId, fleetId, false);
        }
        else
        {
            returnRace = await CreateClubRaceAsync(clubInitials);
        }
        if ((returnRace.FleetOptions?.Count ?? 0) == 1)
        {
            returnRace.FleetId = returnRace.FleetOptions.First().Id;
        }
        returnRace.ClubInitials = clubInitials;
        return returnRace;

    }

    private IList<KeyValuePair<string, string>> GetWeatherIconOptions()
    {
        return _weatherService.GetWeatherIconOptions();
    }

    private async Task<RaceWithOptionsViewModel> CreateClubRaceAsync(string clubInitials)
    {

        var club = await _coreClubService.GetMinimalClub(clubInitials);
        var model = new RaceWithOptionsViewModel
        {
            ClubId = club.Id,
            FleetOptions = await _coreClubService.GetActiveFleets(club.Id),
            SeriesOptions = await _coreSeriesService.GetAllSeriesAsync(club.Id, DateTime.Today, false),
            ScoreCodeOptions = (await _coreScoringService.GetScoreCodesAsync(club.Id))
                .OrderBy(s => s.Name).ToList(),
            CompetitorOptions = new List<Competitor>(),
            CompetitorBoatClassOptions = (await _coreClubService.GetAllBoatClasses(club.Id)).OrderBy(c => c.Name),
            Weather = await _weatherService.GetCurrentWeatherForClubAsync(club),
            WeatherIconOptions = GetWeatherIconOptions(),
            ClubHasCompetitors = await _coreClubService.DoesClubHaveCompetitors(club.Id),
            NeedsLocalDate = true,
            UseAdvancedFeatures = club.UseAdvancedFeatures ?? false
        };

        switch (club.DefaultRaceDateOffset)
        {
            case null:
                model.Date = null;
                model.DefaultRaceDateOffset = null;
                break;
            case 0:
                model.Date = DateTime.Today;
                model.DefaultRaceDateOffset = 0;
                break;
            case -1:
                model.Date = DateTime.Today.AddDays(-1);
                model.DefaultRaceDateOffset = -1;
                break;
            default:
                model.Date = DateTime.Today.AddDays(club.DefaultRaceDateOffset.Value);
                model.DefaultRaceDateOffset = club.DefaultRaceDateOffset;
                break;
        }

        return model;
    }

    private async Task<RaceWithOptionsViewModel> CreateRegattaRaceAsync(
        string clubInitials,
        Guid? regattaId)
    {
        var model = await CreateClubRaceAsync(clubInitials);
        if (!regattaId.HasValue)
        {
            return model;
        }

        var regatta = await _coreRegattaService.GetRegattaAsync(regattaId.Value);

        model.Regatta = _mapper.Map<RegattaSummaryViewModel>(regatta);
        model.FleetOptions = regatta.Fleets;
        if (regatta.ScoringSystemId.HasValue)
        {
            var scoreSystem = await _coreScoringService
                .GetScoringSystemAsync(regatta.ScoringSystemId.Value);
            model.ScoreCodeOptions =
                scoreSystem.ScoreCodes
                    .Union(scoreSystem.InheritedScoreCodes)
                    .OrderBy(s => s.Name)
                    .ToList();
        }
        else
        {
            model.ScoreCodeOptions = (await _coreScoringService.GetScoreCodesAsync(model.ClubId))
                .OrderBy(s => s.Name).ToList();
        }
        model.CompetitorOptions = regatta.Fleets?.FirstOrDefault()?.Competitors;
        if (regatta.StartDate.HasValue && regatta.EndDate.HasValue
                                       && DateTime.Today >= regatta.StartDate && DateTime.Today <= regatta.EndDate)
        {
            model.Date = DateTime.Today;
        }
        else if (regatta.StartDate.HasValue)
        {
            model.Date = regatta.StartDate.Value;
            model.NeedsLocalDate = false;
            if (regatta.StartDate.Value.Date != DateTime.Today.Date)
            {
                model.Weather = default;
            }
        } // otherwise use the default date for typical club races, already filled in. 
        return model;
    }


    private async Task<RaceWithOptionsViewModel> CreateSeriesRaceAsync(
        string clubInitials,
        Guid seriesId)
    {
        var model = await CreateClubRaceAsync(clubInitials);
        var series = await _coreSeriesService.GetOneSeriesAsync(seriesId);
        if(series.Season.Start > DateTime.Now || series.Season.End < DateTime.Now)
        {
            if (series.Races.Any(r => r.Date.HasValue))
            {
                model.Date = series.Races.Where(r => r.Date.HasValue).Max(r => r.Date);
            } else // no races, set to start of season.
            {
                model.Date = series.Season.Start;
            }
        }


        if (series.Races.Any())
        {
            model.FleetId = series.Races.OrderByDescending(r => r.Date)
                                .ThenByDescending(r => r.Order).First().Fleet?.Id
                            ?? Guid.Empty;
        }

        model.SeriesIds = new List<Guid>
        {
            seriesId
        };
        if (series.ScoringSystemId.HasValue)
        {
            var scoreSystem = await _coreScoringService
                .GetScoringSystemAsync(series.ScoringSystemId.Value);
            model.ScoreCodeOptions =
                scoreSystem.ScoreCodes
                    .Union(scoreSystem.InheritedScoreCodes)
                    .OrderBy(s => s.Name)
                    .ToList();
        }
        else
        {
            model.ScoreCodeOptions = (await _coreScoringService.GetScoreCodesAsync(model.ClubId))
                .OrderBy(s => s.Name).ToList();
        }

        return model;
    }

    public async Task AddOptionsToRace(RaceWithOptionsViewModel raceWithOptions)
    {
        if (raceWithOptions.RegattaId.HasValue)
        {
            raceWithOptions.FleetOptions = await _coreClubService.GetAllFleets(raceWithOptions.ClubId);
        }
        else
        {
            raceWithOptions.FleetOptions = await _coreClubService.GetActiveFleets(raceWithOptions.ClubId);
        }
        raceWithOptions.FleetOptions = raceWithOptions.FleetOptions.OrderBy(f => f.ShortName).ToList();

        raceWithOptions.SeriesOptions = await _coreSeriesService.GetAllSeriesAsync(
            raceWithOptions.ClubId,
            raceWithOptions.Date ?? DateTime.Today,
            true);

        raceWithOptions.ScoreCodeOptions = (await _coreScoringService.GetScoreCodesAsync(raceWithOptions.ClubId))
            .OrderBy(s => s.Name).ToList();
        // CompetitorOptions should be set by the JS on the page at edit time.
        raceWithOptions.CompetitorBoatClassOptions =
            (await _coreClubService.GetAllBoatClasses(raceWithOptions.ClubId)).OrderBy(c => c.Name);
        raceWithOptions.WeatherIconOptions = _weatherService.GetWeatherIconOptions();

    }

    public async Task<RaceViewModel> GetSingleRaceDetailsAsync(string clubInitials, Guid id)
    {

        var coreRace = await _coreRaceService.GetRaceAsync(id);
        if (coreRace == null)
        {
            return null;
        }
        var retRace = _mapper.Map<RaceViewModel>(coreRace);
        retRace.Scores = retRace.Scores
            .OrderBy(s => (s.Place == null || s.Place == 0) ? int.MaxValue : s.Place)
            .ThenBy(s => s.Code)
            .ToList();

        var scoreCodes = await _coreScoringService.GetScoreCodesAsync(coreRace.ClubId);
        foreach (var score in retRace.Scores)
        {
            score.ScoreCode = GetScoreCode(score.Code, scoreCodes);
        }
        retRace.Weather = await _weatherService.ConvertToLocalizedWeather(coreRace.Weather, coreRace.ClubId);
        retRace.Regatta = await GetRegatta(retRace);

        return retRace;
    }

    private async Task<RegattaViewModel> GetRegatta(RaceViewModel retRace)
    {
        var regatta = await _coreRegattaService.GetRegattaForRace(retRace.Id);

        return _mapper.Map<RegattaViewModel>(regatta);
    }

    private static void CleanUpRaceTimes(RaceWithOptionsViewModel race)
    {
        // Clean up StartTime to be on the Race Date
        if (race.Date.HasValue && race.StartTime.HasValue)
        {
            var timeOfDay = race.StartTime.Value.TimeOfDay;
            race.StartTime = race.Date.Value.Date + timeOfDay;
        }


        // Clean up FinishTimes to be in the 24 hours after StartTime
        // of, if TrackTimes is not set, clear Finish and elapsed times
        if (race.Scores != null && race.Scores.Count > 0)
        {
            foreach (var score in race.Scores)
            {
                if (!(race.TrackTimes ?? false))
                {
                    score.FinishTime = null;
                    score.ElapsedTime = null;
                    continue;
                }
                else if (score.FinishTime.HasValue)
                {
                    TimeSpan finishTimeOfDay = score.FinishTime.Value.TimeOfDay;
                    if (race.StartTime.HasValue)
                    {
                        var startDate = race.StartTime.Value.Date;
                        var newFinish = startDate + finishTimeOfDay;
                        // If finish is before start, assume it's next day
                        if (newFinish < race.StartTime.Value)
                        {
                            newFinish = newFinish.AddDays(1);
                        }
                        score.FinishTime = newFinish;
                    }
                    else if (race.Date.HasValue)
                    {
                        score.FinishTime = race.Date.Value.Date + finishTimeOfDay;
                    }
                }
            }
        }
    }

    public async Task SaveAsync(RaceWithOptionsViewModel race)
    {
        CleanUpRaceTimes(race);
        await EnsureSeasonExists(race.ClubId, race.Date);
        var fleets = await _coreClubService.GetAllFleets(race.ClubId);
        var series = await _coreSeriesService.GetAllSeriesAsync(race.ClubId, race.Date, false);

        // fill in series and fleets
        if (race.SeriesIds != null)
        {
            race.Series = series.Where(s => race.SeriesIds.Contains(s.Id)).ToList();
        }
        if (race.FleetId != default)
        {
            race.Fleet = fleets.Single(f => f.Id == race.FleetId);
            // if a regatta race, give everyone in the fleet a result
            if (race.RegattaId.HasValue)
            {
                foreach (var competitor in race.Fleet.Competitors)
                {
                    if (!race.Scores?.Any(s => s.CompetitorId == competitor.Id) ?? false)
                    {
                        race.Scores.Add(new ScoreViewModel
                        {
                            CompetitorId = competitor.Id,
                            Code = "DNC",
                            Race = _mapper.Map<Race>(race)
                        });
                    }
                }
            }
        }
        if (race.Order == 0)
        {
            if (race.InitialOrder.HasValue && race.InitialOrder.Value != 0)
            {
                race.Order = race.InitialOrder.Value;
            }
            else
            {
                race.Order = await _coreRaceService.GetNewRaceNumberAsync(
                    race.ClubId,
                    race.FleetId,
                    race.Date,
                    race.RegattaId
                    );
            }
        }
        var raceDto = _mapper.Map<RaceDto>(race);
        if ((raceDto.SeriesIds?.Count ?? 0) != (race?.SeriesIds?.Count ?? 0))
        {
            raceDto.SeriesIds = race.SeriesIds;
        }
        if (race.Weather != null)
        {
            var weatherSettings = (await _coreClubService.GetMinimalClub(race.ClubId)).WeatherSettings;
            if (String.IsNullOrWhiteSpace(race.Weather.WindSpeedUnits))
            {
                race.Weather.WindSpeedUnits = weatherSettings?.WindSpeedUnits;
            }
            if (String.IsNullOrWhiteSpace(race.Weather.TemperatureUnits))
            {
                race.Weather.TemperatureUnits = weatherSettings?.TemperatureUnits;
            }
        }

        var weather = _weatherService.GetStandardWeather(race.Weather);

        raceDto.Weather = _mapper.Map<WeatherDto>(weather);

        var raceId = await _coreRaceService.SaveAsync(raceDto);

        race.Id = raceId;
        if (race.RegattaId.HasValue)
        {
            await _coreRegattaService.AddRaceToRegattaAsync(
                _mapper.Map<Race>(race), race.RegattaId.Value);
        }
    }

    
    private async Task EnsureSeasonExists(
        Guid clubId,
        DateTime? raceDate)
    {
        // raceDate should not be null, but creating a season for today
        // shouldn't cause much trouble.

        var dateToUse = raceDate ?? DateTime.UtcNow;
        var seasons = await _coreSeasonService.GetSeasons(clubId);

        if (!seasons.Any(s => s.Start <= raceDate && s.End >= raceDate))
        {
            await CreateSeasonForDate(clubId, dateToUse, seasons);
        }
    }

    private async Task CreateSeasonForDate(
        Guid clubId,
        DateTime dateToUse,
        IEnumerable<Season> existingSeasons)
    {
        var lastSeason = existingSeasons.OrderByDescending(s => s.Start).FirstOrDefault();

        Season newSeason = null;
        //if no seasons exist
        if (lastSeason == null)
        {
            newSeason = new Season
            {
                Start = new DateTime(dateToUse.Year, 1, 1),
                End = new DateTime(dateToUse.Year, 12, 31),
                Name = dateToUse.Year.ToString(CultureInfo.InvariantCulture),
                ClubId = clubId
            };
        }
        else
        {
            int direction = lastSeason.Start < dateToUse ? 1 : -1;
            int yearCounter = direction;
            while (newSeason == null)
            {
                if (lastSeason.Start.AddYears(yearCounter) <= dateToUse
                    && lastSeason.End.AddYears(yearCounter) >= dateToUse)
                {
                    newSeason = new Season
                    {
                        Start = lastSeason.Start.AddYears(yearCounter),
                        End = lastSeason.End.AddYears(yearCounter),
                        ClubId = clubId
                    };
                }

                // move one more, future or past. 
                yearCounter += direction;
            }

            yearCounter -= direction;
            // now, starting at the same place, look for a name. hopefully first try finds it.
            while (string.IsNullOrWhiteSpace(newSeason.Name))
            {
                var proposedName = lastSeason.Start.AddYears(yearCounter).Year
                    .ToString(CultureInfo.InvariantCulture);
                if (!existingSeasons.Any(s => s.Name == proposedName))
                {
                    newSeason.Name = proposedName;
                }
             
                // move one more, future or past. 
                yearCounter += direction;
            }
        }

        EnsureSeasonDatesDoNotOverlap(newSeason, existingSeasons, dateToUse);

        await _coreSeasonService.SaveNew(newSeason);

    }

    private void EnsureSeasonDatesDoNotOverlap(
        Season newSeason,
        IEnumerable<Season> existingSeasons,
        DateTime mustIncludeDate)
    {
        var seasonBefore = existingSeasons.OrderByDescending(s => s.End)
            .FirstOrDefault(s => s.End < mustIncludeDate);
        if (seasonBefore != null)
        {
            newSeason.Start = seasonBefore.End > newSeason.Start ? seasonBefore.End.AddDays(1) : newSeason.Start;
        }

        var seasonAfter = existingSeasons.OrderBy(s => s.Start)
            .FirstOrDefault(s => s.Start > mustIncludeDate);
        if (seasonAfter != null)
        {
            newSeason.End = seasonAfter.Start < newSeason.End ? seasonAfter.Start.AddDays(-1) : newSeason.End;
        }
    }

    public async Task<Season> GetCurrentSeasonAsync(string clubInitials)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        return await _coreRaceService.GetMostRecentRaceSeasonAsync(clubId);
    }

    public async Task<RaceWithOptionsViewModel> FixupRaceWithOptions(
        string clubInitials,
        RaceWithOptionsViewModel race)
    {
        var blankRace = await GetBlankRaceWithOptions(
            clubInitials,
            race.RegattaId,
            race.SeriesIds?.FirstOrDefault(),
            race.FleetId);
        race.ScoreCodeOptions = blankRace.ScoreCodeOptions;
        race.FleetOptions = blankRace.FleetOptions;
        race.CompetitorBoatClassOptions = blankRace.CompetitorBoatClassOptions;
        race.SeriesOptions = blankRace.SeriesOptions;
        race.WeatherIconOptions = blankRace.WeatherIconOptions;
        race.UseAdvancedFeatures = blankRace.UseAdvancedFeatures;
        foreach (var score in race.Scores)
        {
            score.Competitor = race.CompetitorOptions.First(c => c.Id == score.CompetitorId);
        }

        return race;
    }
}