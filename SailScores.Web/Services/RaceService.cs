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
        private readonly IMapper _mapper;

        public RaceService(
            Core.Services.IClubService clubService,
            Core.Services.IRaceService coreRaceService,
            Core.Services.IScoringService coreScoringService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreRaceService = coreRaceService;
            _coreScoringService = coreScoringService;
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
                .ThenBy(r => r.Fleet.Name)
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

        public async Task<RaceWithOptionsViewModel> GetBlankRaceWithOptions(string clubInitials)
        {
            var club = await _coreClubService.GetFullClub(clubInitials);
            var model = new RaceWithOptionsViewModel();
            model.ClubId = club.Id;
            model.FleetOptions = club.Fleets;
            model.SeriesOptions = club.Series;
            model.ScoreCodeOptions = (await _coreScoringService.GetScoreCodesAsync(club.Id))
                .OrderBy(s => s.Name).ToList();
            model.CompetitorOptions = club.Competitors;
            model.Date = DateTime.Today;
            model.Order = 1;
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
            }
            if(race.Order == 0)
            {
                var maxOrder = club.Races
                    .Where(r =>
                        r.Date == race.Date
                        && r.Fleet.Id == race.FleetId)
                    .Max(r => r.Order);
                race.Order = maxOrder + 1;
            }
            var raceDto = _mapper.Map<RaceDto>(race);
            await _coreRaceService.SaveAsync(raceDto);
        }
    }
}
