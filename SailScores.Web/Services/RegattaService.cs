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
        private readonly IMapper _mapper;

        public RegattaService(
            Core.Services.IClubService clubService,
            IMapper mapper)
        {
            _clubService = clubService;
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
    }
}
