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
        private readonly IMapper _mapper;

        public RaceService(
            Core.Services.IClubService clubService,
            Core.Services.IRaceService coreRaceService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreRaceService = coreRaceService;
            _mapper = mapper;
        }

        public async Task Delete(Guid id)
        {
            await _coreRaceService.Delete(id);
        }

        public async Task<IEnumerable<RaceSummaryViewModel>> GetAllRaceSummariesAsync(string clubInitials)
        {
            var club = (await _coreClubService.GetClubs(true)).First(c => c.Initials == clubInitials);
            var races = (await _coreRaceService.GetFullRacesAsync(club.Id))
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.Fleet.Name)
                .ThenBy(r => r.Order);

            var vm = _mapper.Map<List<RaceSummaryViewModel>>(races);
            return vm;
        }

        public async Task<RaceWithOptionsViewModel> GetBlankRaceWithOptions(string clubInitials)
        {
            var club = await _coreClubService.GetFullClub(clubInitials);
            var model = new RaceWithOptionsViewModel();
            model.FleetOptions = club.Fleets;
            model.SeriesOptions = club.Series;
            model.ScoreCodeOptions = club.ScoreCodes;
            model.CompetitorOptions = club.Competitors;
            return model;

        }

        public async Task<RaceViewModel> GetSingleRaceDetailsAsync(string clubInitials, Guid id)
        {

            var raceDto = await _coreRaceService.GetRaceAsync(id);
            var retRace = _mapper.Map<RaceViewModel>(raceDto);
            retRace.Scores = retRace.Scores
                .OrderBy(s => (s.Place == null || s.Place == 0) ? int.MaxValue : s.Place)
                .ThenBy(s => s.Code)
                .ToList();

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

            var raceDto = _mapper.Map<RaceDto>(race);
            await _coreRaceService.SaveAsync(raceDto);
        }
    }
}
