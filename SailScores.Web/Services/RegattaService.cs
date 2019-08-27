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
    public class RegattaService : IRegattaService
    {
        private readonly Core.Services.IClubService _clubService;
        private readonly Core.Services.IRegattaService _coreRegattaService;
        private readonly IMapper _mapper;

        public RegattaService(
            Core.Services.IClubService clubService,
            Core.Services.IRegattaService coreRegattaService,
            IMapper mapper)
        {
            _clubService = clubService;
            _coreRegattaService = coreRegattaService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<RegattaSummary>> GetAllRegattaSummaryAsync(string clubInitials)
        {
            var coreObject = await _clubService.GetFullClub(clubInitials);
            var orderedRegattas = coreObject.Regattas
                .OrderByDescending(s => s.Season.Start)
                .ThenBy(s => s.StartDate)
                .ThenBy(s => s.Name);
            return _mapper.Map<IList<RegattaSummary>>(orderedRegattas);
        }

        public async Task<Regatta> GetRegattaAsync(string clubInitials, string season, string regattaName)
        {
            return await _coreRegattaService.GetRegattaAsync(clubInitials, season, regattaName);
        }

        public async Task SaveNewAsync(RegattaWithOptionsViewModel model)
        {
            await PrepRegattaVmAsync(model);
            await _coreRegattaService.SaveNewRegattaAsync(model);
        }

        private async Task PrepRegattaVmAsync(RegattaWithOptionsViewModel model)
        {
            var club = await _clubService.GetFullClub(model.ClubId);
            if (model.StartDate.HasValue)
            {
                model.Season = club.Seasons.Single(s => s.Start <= model.StartDate.Value
                && s.End >= model.StartDate.Value);
            }
            if (model.ScoringSystemId == Guid.Empty)
            {
                model.ScoringSystemId = null;
            }
            model.Fleets = new List<Fleet>();
            foreach (var fleetId in model.FleetIds)
            {
                model.Fleets.Add(club.Fleets
                    .Single(f => f.Id == fleetId));
            }
        }

        public async Task UpdateAsync(RegattaWithOptionsViewModel model)
        {
            await PrepRegattaVmAsync(model);
            await _coreRegattaService.UpdateAsync(model);
        }

        public async Task DeleteAsync(Guid regattaId)
        {
            await _coreRegattaService.DeleteAsync(regattaId);
        }
    }
}
