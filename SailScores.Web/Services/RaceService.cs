using AutoMapper;
using SailScores.Api.Dtos;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class RaceService : IRaceService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly Core.Services.IRaceService _coreRaceService;
        private readonly Core.Services.ISeriesService _coreSeriesService;
        private readonly IScoringService _coreScoringService;
        private readonly Core.Services.IRegattaService _coreRegattaService;
        private readonly IWeatherService _weatherService;
        private readonly IMapper _mapper;

        public RaceService(
            Core.Services.IClubService clubService,
            Core.Services.IRaceService coreRaceService,
            Core.Services.ISeriesService coreSeriesService,
            Core.Services.IScoringService coreScoringService,
            Core.Services.IRegattaService coreRegattaService,
            IWeatherService weatherService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreRaceService = coreRaceService;
            _coreSeriesService = coreSeriesService;
            _coreScoringService = coreScoringService;
            _coreRegattaService = coreRegattaService;
            _weatherService = weatherService;
            _mapper = mapper;
        }

        public async Task Delete(Guid id)
        {
            await _coreRaceService.Delete(id);
        }

        public async Task<IEnumerable<RaceSummaryViewModel>> GetAllRaceSummariesAsync(
            string clubInitials,
            bool includeScheduled,
            bool includeAbandoned)
        {
            var club = (await _coreClubService.GetClubs(true)).First(c => c.Initials == clubInitials);
            var races = (await _coreRaceService.GetFullRacesAsync(club.Id, includeScheduled, includeAbandoned))
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.Fleet?.Name)
                .ThenBy(r => r.Order);

            var scoreCodes = await _coreScoringService.GetScoreCodesAsync(club.Id);
            var vm = _mapper.Map<List<RaceSummaryViewModel>>(races);
            foreach(var race in vm)
            {
                foreach(var score in race.Scores)
                {
                    score.ScoreCode = GetScoreCode(score.Code, scoreCodes);
                }
            }
            return vm;
        }

        private static ScoreCode GetScoreCode(string code, IEnumerable<ScoreCode> scoreCodes)
        {
            return scoreCodes.FirstOrDefault(sc => sc.Name == code);
        }

        public async Task<RaceWithOptionsViewModel> GetBlankRaceWithOptions(
            string clubInitials,
            Guid? regattaId,
            Guid? seriesId)
        {
            RaceWithOptionsViewModel returnRace;
            if (regattaId.HasValue)
            {
                returnRace = await CreateRegattaRaceAsync(clubInitials, regattaId);
            } else if (seriesId.HasValue)
            {
                returnRace = await CreateSeriesRaceAsync(clubInitials, seriesId.Value);
            }
            else
            {
                returnRace = await CreateClubRaceAsync(clubInitials);
            }
            returnRace.ClubInitials = clubInitials;
            return returnRace;

        }

        private IList<KeyValuePair<string, string>> GetWeatherIconOptions()
        {
            return _weatherService.GetWeatherIconOptions();
        }

        private async Task<WeatherViewModel> GetCurrentWeatherAsync(Guid clubId)
        {
            return await _weatherService.GetCurrentWeatherForClubAsync(clubId);
        }
        private async Task<RaceWithOptionsViewModel> CreateClubRaceAsync(string clubInitials)
        {
            var clubId = await _coreClubService.GetClubId(clubInitials);
            var model = new RaceWithOptionsViewModel
            {
                ClubId = clubId,
                FleetOptions = await _coreClubService.GetAllFleets(clubId),
                SeriesOptions = await _coreSeriesService.GetAllSeriesAsync(clubId, DateTime.Today, false),
                ScoreCodeOptions = (await _coreScoringService.GetScoreCodesAsync(clubId))
                    .OrderBy(s => s.Name).ToList(),
                CompetitorOptions = new List<Competitor>(),
                CompetitorBoatClassOptions = (await _coreClubService.GetAllBoatClasses(clubId)).OrderBy(c => c.Name),
                Date = DateTime.Today,
                Weather = (await GetCurrentWeatherAsync(clubId)),
                WeatherIconOptions = GetWeatherIconOptions(),
                ClubHasCompetitors = await _coreClubService.DoesClubHaveCompetitors(clubId)
            };
            return model;
        }

        private async Task<RaceWithOptionsViewModel> CreateRegattaRaceAsync(
            string clubInitials,
            Guid? regattaId)
        {
            var model = await CreateClubRaceAsync(clubInitials);
            if(!regattaId.HasValue)
            {
                return model;
            }
            //var club = await _coreClubService.GetFullClub(clubInitials);
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
            }
            else
            {
                model.Date = DateTime.Today;
            }
            return model;
        }


        private async Task<RaceWithOptionsViewModel> CreateSeriesRaceAsync(
            string clubInitials,
            Guid seriesId)
        {
            var model = await CreateClubRaceAsync(clubInitials);

            //var club = await _coreClubService.GetFullClub(clubInitials);
            var series = await _coreSeriesService.GetOneSeriesAsync(seriesId);
            
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
            var club = await _coreClubService.GetFullClub(raceWithOptions.ClubId);
            raceWithOptions.FleetOptions = club.Fleets;
            raceWithOptions.SeriesOptions = club.Series;
            raceWithOptions.ScoreCodeOptions = (await _coreScoringService.GetScoreCodesAsync(club.Id))
                .OrderBy(s => s.Name).ToList();
            raceWithOptions.CompetitorOptions = club.Competitors;
            raceWithOptions.CompetitorBoatClassOptions = club.BoatClasses.OrderBy(c => c.Name);
            raceWithOptions.WeatherIconOptions = _weatherService.GetWeatherIconOptions();
        }

        public async Task<RaceViewModel> GetSingleRaceDetailsAsync(string clubInitials, Guid id)
        {

            var coreRace = await _coreRaceService.GetRaceAsync(id);
            if(coreRace == null)
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
            retRace.Weather = await _weatherService.ConvertToLocalizedWeather(coreRace.Weather, coreRace.ClubId );

            return retRace;
        }

        public async Task SaveAsync(RaceWithOptionsViewModel race)
        {
            var fleets = await _coreClubService.GetAllFleets(race.ClubId);
            var series = await _coreSeriesService.GetAllSeriesAsync(race.ClubId, DateTime.Today, false);

            // fill in series and fleets
            if (race.SeriesIds != null) {
                race.Series = series.Where(s => race.SeriesIds.Contains(s.Id)).ToList();
            }
            if(race.FleetId != default(Guid))
            {
                race.Fleet = fleets.Single(f => f.Id == race.FleetId);
                // if a regatta race, give everyone in the fleet a result
                if (race.RegattaId.HasValue)
                {
                    foreach (var competitor in race.Fleet.Competitors)
                    {
                        if (!race.Scores.Any(s => s.CompetitorId == competitor.Id))
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
            if(race.Order == 0)
            {
                if (race.InitialOrder.HasValue && race.InitialOrder.Value != 0)
                {
                    race.Order = race.InitialOrder.Value;
                }
                else
                {
                    var races = await _coreRaceService.GetRacesAsync(race.ClubId);
                    var maxOrder = races
                        .Where(r =>
                            r.Date == race.Date
                            && r.Fleet != null
                            && r.Fleet.Id == race.FleetId)
                        .DefaultIfEmpty()
                        .Max(r => r?.Order ?? 0);
                    race.Order = maxOrder + 1;
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
            if(race.RegattaId.HasValue)
            {
                await _coreRegattaService.AddRaceToRegattaAsync(
                    _mapper.Map<Race>(race), race.RegattaId.Value);
            }
        }
    }
}
