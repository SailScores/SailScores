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
        private readonly IScoringService _coreScoringService;
        private readonly Core.Services.IRegattaService _coreRegattaService;
        private readonly IMapper _mapper;

        public RaceService(
            Core.Services.IClubService clubService,
            Core.Services.IRaceService coreRaceService,
            Core.Services.IScoringService coreScoringService,
            Core.Services.IRegattaService coreRegattaService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreRaceService = coreRaceService;
            _coreScoringService = coreScoringService;
            _coreRegattaService = coreRegattaService;
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
            if (regattaId.HasValue)
            {
                return await CreateRegattaRaceAsync(clubInitials, regattaId);
            } else if (seriesId.HasValue)
            {
                return await CreateSeriesRaceAsync(clubInitials, seriesId.Value);
            }
            else
            {
                return await CreateClubRaceAsync(clubInitials);
            }

        }

        private async Task<RaceWithOptionsViewModel> CreateClubRaceAsync(string clubInitials)
        {
            var club = await _coreClubService.GetFullClub(clubInitials);
            var model = new RaceWithOptionsViewModel
            {
                ClubId = club.Id,
                FleetOptions = club.Fleets,
                SeriesOptions = club.Series,
                ScoreCodeOptions = (await _coreScoringService.GetScoreCodesAsync(club.Id))
                    .OrderBy(s => s.Name).ToList(),
                CompetitorOptions = club.Competitors,
                Date = DateTime.Today
            };
            return model;
        }

        private async Task<RaceWithOptionsViewModel> CreateRegattaRaceAsync(
            string clubInitials,
            Guid? regattaId)
        {
            var club = await _coreClubService.GetFullClub(clubInitials);
            var regatta = club.Regattas.Single(r => r.Id == regattaId);
            var model = new RaceWithOptionsViewModel();
            model.ClubId = club.Id;
            model.Regatta = _mapper.Map<RegattaSummaryViewModel>(regatta);
            model.FleetOptions = regatta.Fleets;
            model.SeriesOptions = club.Series;
            if (regatta.ScoringSystemId.HasValue)
            {
                model.ScoreCodeOptions =
                    (await _coreScoringService
                        .GetScoringSystemAsync(regatta.ScoringSystemId.Value))
                    .ScoreCodes;
            }
            else
            {
                model.ScoreCodeOptions = (await _coreScoringService.GetScoreCodesAsync(club.Id))
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
            var club = await _coreClubService.GetFullClub(clubInitials);
            var series = club.Series.Single(r => r.Id == seriesId);
            var model = new RaceWithOptionsViewModel();
            model.ClubId = club.Id;
            model.Date = DateTime.Today;
            model.FleetOptions = club.Fleets;
            model.SeriesOptions = club.Series;
            model.SeriesIds = new List<Guid>
            {
                seriesId
            };
            if (series.ScoringSystemId.HasValue)
            {
                model.ScoreCodeOptions =
                    (await _coreScoringService
                        .GetScoringSystemAsync(series.ScoringSystemId.Value))
                    .ScoreCodes;
            }
            else
            {
                model.ScoreCodeOptions = (await _coreScoringService.GetScoreCodesAsync(club.Id))
                    .OrderBy(s => s.Name).ToList();
            }
            model.CompetitorOptions = club.Competitors;
            
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
        }

        public async Task<RaceViewModel> GetSingleRaceDetailsAsync(string clubInitials, Guid id)
        {

            var raceDto = await _coreRaceService.GetRaceAsync(id);
            if(raceDto == null)
            {
                return null;
            }
            var retRace = _mapper.Map<RaceViewModel>(raceDto);
            retRace.Scores = retRace.Scores
                .OrderBy(s => (s.Place == null || s.Place == 0) ? int.MaxValue : s.Place)
                .ThenBy(s => s.Code)
                .ToList();

            var scoreCodes = await _coreScoringService.GetScoreCodesAsync(raceDto.ClubId);
            foreach (var score in retRace.Scores)
            {
                score.ScoreCode = GetScoreCode(score.Code, scoreCodes);
            }

            return retRace;
        }

        public async Task SaveAsync(RaceWithOptionsViewModel race)
        {
            var club = await _coreClubService.GetFullClub(race.ClubId);
            // fill in series and fleets
            if (race.SeriesIds != null) {
                race.Series = club.Series.Where(s => race.SeriesIds.Contains(s.Id)).ToList();
            }
            if(race.FleetId != default(Guid))
            {
                race.Fleet = club.Fleets.Single(f => f.Id == race.FleetId);
                // if a regatta race, give everyone in the fleet a result
                if (race.RegattaId.HasValue)
                {
                    foreach (var competitor in race.Fleet.Competitors)
                    {
                        if (!race.Scores.Any(s => s.CompetitorId == competitor.Id))
                        {
                            race.Scores.Add(new Score
                            {
                                CompetitorId = competitor.Id,
                                Code = "DNC",
                                Race = race
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
                    var maxOrder = club.Races
                        .Where(r =>
                            r.Date == race.Date
                            && r.Fleet.Id == race.FleetId)
                        .DefaultIfEmpty()
                        .Max(r => r?.Order ?? 0);
                    race.Order = maxOrder + 1;
                }
            }
            var raceDto = _mapper.Map<RaceDto>(race);
            var raceId = await _coreRaceService.SaveAsync(raceDto);

            race.Id = raceId;
            if(race.RegattaId.HasValue)
            {
                await _coreRegattaService.AddRaceToRegattaAsync(race, race.RegattaId.Value);
            }
        }
    }
}
