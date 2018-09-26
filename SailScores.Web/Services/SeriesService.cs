using AutoMapper;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core = SailScores.Core;

namespace SailScores.Web.Services
{
    public class SeriesService : ISeriesService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly IMapper _mapper;

        public SeriesService(
            Core.Services.IClubService clubService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SeriesSummary>> GetAllSeriesSummaryAsync(string clubInitials)
        {
            var coreObject = await _coreClubService.GetFullClub(clubInitials);

            return _mapper.Map<IList<SeriesSummary>>(coreObject.Series);
        }

        public async Task<Core.Model.Series> GetSeriesAsync(string clubInitials, string season, string seriesName)
        {
            var coreObject = await _coreClubService.GetFullClub(clubInitials);

            var series = coreObject.Series.FirstOrDefault(s =>
                s.Season.Name == season && s.Name == seriesName);

            return series;
        }
    }
}
