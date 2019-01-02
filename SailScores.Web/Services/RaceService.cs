using AutoMapper;
using Sailscores.Core.Model;
using Sailscores.Core.Services;
using Sailscores.Web.Models.Sailscores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sailscores.Web.Services
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

        public async Task<RaceViewModel> GetSingleRaceDetailsAsync(string clubInitials, Guid id)
        {

            var raceDto = await _coreRaceService.GetRaceAsync(id);
            var retRace = _mapper.Map<RaceViewModel>(raceDto);
            retRace.Scores = retRace.Scores.OrderBy(s => s.Place ?? int.MaxValue).ToList();

            return retRace;
        }
    }
}
