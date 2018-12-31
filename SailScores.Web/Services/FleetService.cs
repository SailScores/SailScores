using AutoMapper;
using Sailscores.Core.Model;
using Sailscores.Web.Models.Sailscores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core = Sailscores.Core;

namespace Sailscores.Web.Services
{
    public class FleetService : IFleetService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly IMapper _mapper;

        public FleetService(
            Core.Services.IClubService clubService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _mapper = mapper;
        }

        public async Task<IList<FleetSummary>> GetAllFleetSummaryAsync(string clubInitials)
        {
            var coreObject = await _coreClubService.GetFullClub(clubInitials);

            return _mapper.Map<IList<FleetSummary>>(coreObject.Fleets);
        }

        public async Task<FleetSummary> GetFleetAsync(string clubInitials, string season, string fleetShortName)
        {
            var coreObject = await _coreClubService.GetFullClub(clubInitials);
            var retFleet = _mapper.Map<FleetSummary>(coreObject.Fleets.First(f => f.ShortName == fleetShortName));

            retFleet.Series = coreObject.Series.Where(s => s.Races.Any(r => r.Fleet.Id == retFleet.Id)).ToList();

            return retFleet;
        }
    }
}
