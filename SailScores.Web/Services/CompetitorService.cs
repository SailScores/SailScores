using AutoMapper;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SailScores.Api.Dtos;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class CompetitorService : ICompetitorService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly Core.Services.ICompetitorService _coreCompetitorService;
        private readonly IMapper _mapper;

        public CompetitorService(
            Core.Services.IClubService clubService,
            Core.Services.ICompetitorService competitorService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreCompetitorService = competitorService;
            _mapper = mapper;
        }

        public async Task DeleteCompetitorAsync(Guid competitorId)
        {
            await _coreCompetitorService.DeleteCompetitorAsync(competitorId);
        }

        public Task<Competitor> GetCompetitorAsync(Guid competitorId)
        {
            var comp = _coreCompetitorService.GetCompetitorAsync(competitorId);
            return comp;
        }

        public async Task<Competitor> GetCompetitorAsync(string clubInitials, string sailNumber)
        {
            var club = await _coreClubService.GetMinimalClub(clubInitials);
            var comps = await _coreCompetitorService.GetCompetitorsAsync(club.Id, null);
            return comps.FirstOrDefault(c => String.Equals(c.SailNumber, sailNumber, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<CompetitorStatsViewModel> GetCompetitorStatsAsync(
            string clubInitials,
            string sailNumber)
        {

            var club = await _coreClubService.GetMinimalClub(clubInitials);
            var comps = await _coreCompetitorService.GetCompetitorsAsync(club.Id, null);

            var comp = comps.FirstOrDefault(c => String.Equals(c.SailNumber, sailNumber, StringComparison.OrdinalIgnoreCase));
            if(comp == null)
            {
                return null;
            }
            var vm = _mapper.Map<CompetitorStatsViewModel>(comp);

            vm.SeasonStats = await _coreCompetitorService.GetCompetitorStatsAsync(clubInitials, sailNumber);

            return vm;
        }

        public async Task<List<PlaceCount>> GetCompetitorSeasonRanksAsync(
            Guid competitorId,
            string seasonName)
        {

            var seasonStats = await _coreCompetitorService.GetCompetitorSeasonRanksAsync(
                competitorId,
                seasonName);

            var vm = _mapper.Map<List<PlaceCount>>(seasonStats);
            
            return vm;
        }

        public async Task SaveAsync(
            MultipleCompetitorsWithOptionsViewModel vm,
            Guid clubId)
        {
            var coreCompetitors = new List<Core.Model.Competitor>();
            var fleets = (await _coreClubService.GetMinimalForSelectedBoatsFleets(clubId))
                .OrderBy(f => f.Name);
            foreach (var comp in vm.Competitors)
            {
                // if they didn't give a name or sail, skip this row.
                if (String.IsNullOrWhiteSpace(comp.Name)
                    && String.IsNullOrWhiteSpace(comp.SailNumber)
                    )
                {
                    break;
                }
                var currentComp = _mapper.Map<Core.Model.Competitor>(comp);
                currentComp.ClubId = clubId;
                currentComp.Fleets = new List<Fleet>();
                currentComp.BoatClassId = vm.BoatClassId;

                if (vm.FleetIds != null)
                {
                    foreach (var fleetId in vm.FleetIds)
                    {
                        currentComp.Fleets.Add(fleets.Single(f => f.Id == fleetId));
                    }
                }
                coreCompetitors.Add(currentComp);
            }

            foreach (var comp in coreCompetitors)
            {
                await _coreCompetitorService.SaveAsync(comp);
            }
        }

        public async Task SaveAsync(CompetitorWithOptionsViewModel competitor)
        {

            if (competitor.Fleets == null)
            {
                competitor.Fleets = new List<Fleet>();
            }
            if (competitor.FleetIds == null)
            {
                competitor.FleetIds = new List<Guid>();
            }

            var fleets = (await _coreClubService.GetMinimalForSelectedBoatsFleets(
                    competitor.ClubId))
                .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats);

            foreach (var fleetId in competitor.FleetIds)
            {
                competitor.Fleets.Add(fleets.Single(f => f.Id == fleetId));
            }
            await _coreCompetitorService.SaveAsync(competitor);
        }

        public async Task<Guid?> GetCompetitorIdForSailnumberAsync(Guid clubId, string sailNumber)
        {
            var competitor = await _coreCompetitorService.GetCompetitorBySailNumberAsync(clubId, sailNumber);
            return competitor?.Id;
        }
    }
}
