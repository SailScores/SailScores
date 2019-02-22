using AutoMapper;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core = SailScores.Core;

namespace SailScores.Web.Services
{
    public class FleetService : IFleetService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly Core.Services.IFleetService _coreFleetService;
        private readonly IMapper _mapper;

        public FleetService(
            Core.Services.IClubService clubService,
            Core.Services.IFleetService coreFleetService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreFleetService = coreFleetService;
            _mapper = mapper;
        }

        public async Task<IList<FleetSummary>> GetAllFleetSummary(string clubInitials)
        {
            var coreObject = await _coreClubService.GetFullClub(clubInitials);

            return _mapper.Map<IList<FleetSummary>>(coreObject.Fleets);
        }

        public async Task<FleetSummary> GetFleet(string clubInitials, string fleetShortName)
        {
            var coreObject = await _coreClubService.GetFullClub(clubInitials);
            var retFleet = _mapper.Map<FleetSummary>(coreObject.Fleets.First(f => f.ShortName == fleetShortName));

            retFleet.Series = coreObject.Series.Where(s => s.Races.Any(r => r.Fleet.Id == retFleet.Id)).ToList();

            return retFleet;
        }
        public async Task<Fleet> GetFleet(Guid fleetId)
        {
            var coreObject = await _coreFleetService.Get(fleetId);
            return coreObject;
        }

        public async Task Delete(Guid fleetId)
        {
            await _coreFleetService.Delete(fleetId);
        }

        public async Task SaveNew(FleetCreateViewModel fleet)
        {
            var coreModel = _mapper.Map<Fleet>(fleet);
            var club = await _coreClubService.GetFullClub(fleet.ClubId);
            if (fleet.FleetType == Api.Enumerations.FleetType.SelectedClasses
                && fleet.BoatClassIds != null)
            {
                coreModel.BoatClasses =
                    club.BoatClasses
                    .Where(c => fleet.BoatClassIds.Contains(c.Id))
                    .ToList();
            }
            await _coreFleetService.SaveNew(coreModel);
        }

        public async Task Update(FleetWithOptionsViewModel fleet)
        {
            var coreModel = _mapper.Map<Fleet>(fleet);
            var club = await _coreClubService.GetFullClub(fleet.ClubId);
            if (fleet.FleetType == Api.Enumerations.FleetType.SelectedClasses
                && fleet.BoatClassIds != null)
            {
                coreModel.BoatClasses =
                    club.BoatClasses
                    .Where(c => fleet.BoatClassIds.Contains(c.Id))
                    .ToList();
            }
            else if (fleet.FleetType == Api.Enumerations.FleetType.SelectedBoats
                    && fleet.CompetitorIds != null)
            {
                coreModel.Competitors =
                    club.Competitors
                    .Where(c => fleet.CompetitorIds.Contains(c.Id))
                    .ToList();
            }
            await _coreFleetService.Update(coreModel);
        }

    }
}
