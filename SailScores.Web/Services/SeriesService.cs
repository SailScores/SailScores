using AutoMapper;
using Sailscores.Web.Models.Sailscores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core = Sailscores.Core;

namespace Sailscores.Web.Services
{
    public class SeriesService : ISeriesService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly Core.Services.ISeriesService _coreSeriesService;
        private readonly IMapper _mapper;

        public SeriesService(
            Core.Services.IClubService clubService,
            Core.Services.ISeriesService seriesService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreSeriesService = seriesService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SeriesSummary>> GetAllSeriesSummaryAsync(string clubInitials)
        {
            var coreObject = await _coreClubService.GetFullClub(clubInitials);

            return _mapper.Map<IList<SeriesSummary>>(coreObject.Series);
        }

        public async Task<Core.Model.Series> GetSeriesAsync(string clubInitials, string season, string seriesName)
        {
            var series = await _coreSeriesService.GetSeriesDetailsAsync(clubInitials, season, seriesName );

            return series;
        }
    }
}
