using AutoMapper;
using SailScores.Core.Model;
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
        private readonly IMapper _mapper;

        public RaceService(
            Core.Services.IClubService clubService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Race>> GetAllRaceSummariesAsync(string clubInitials)
        {
            var club = await _coreClubService.GetFullClub(clubInitials);

            return club.Races;
            //var vm = _mapper.Map<List<RaceSummaryViewModel>>(club.Races);
            //return vm;
        }

        public async Task<RaceViewModel> GetSingleRaceDetailsAsync(string clubInitials, Guid id)
        {

            var club = await _coreClubService.GetFullClub(clubInitials);
            return _mapper.Map<RaceViewModel>(club.Races.First(r => r.Id == id));

        }
    }
}
