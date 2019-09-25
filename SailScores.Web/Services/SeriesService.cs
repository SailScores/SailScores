using AutoMapper;
using SailScores.Core.Model;
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

        public async Task DeleteAsync(Guid id)
        {
            await _coreSeriesService.Delete(id);
        }

        public async Task<IEnumerable<SeriesSummary>> GetNonRegattaSeriesSummariesAsync(string clubInitials)
        {
            var clubId = await _coreClubService.GetClubId(clubInitials);
            var coreObject = await _coreSeriesService.GetAllSeriesAsync(clubId, null, false);
            var orderedSeries = 
                coreObject
                .OrderByDescending(s => s.Season.Start)
                .ThenBy(s => s.Name);
            return _mapper.Map<IList<SeriesSummary>>(orderedSeries);
        }

        public async Task<Core.Model.Series> GetSeriesAsync(string clubInitials, string season, string seriesUrlName)
        {
            var series = await _coreSeriesService.GetSeriesDetailsAsync(clubInitials, season, seriesUrlName );

            return series;
        }

        public async Task SaveNew(SeriesWithOptionsViewModel model)
        {
            var club = await _coreClubService.GetFullClub(model.ClubId);
            var season = club.Seasons.Single(s => s.Id == model.SeasonId);
            model.Season = season;
            if(model.ScoringSystemId == Guid.Empty)
            {
                model.ScoringSystemId = null;
            }
            await _coreSeriesService.SaveNewSeries(model);
        }

        public async Task Update(SeriesWithOptionsViewModel model)
        {
            var club = await _coreClubService.GetFullClub(model.ClubId);
            var season = club.Seasons.Single(s => s.Id == model.SeasonId);
            model.Season = season;
            if (model.ScoringSystemId == Guid.Empty)
            {
                model.ScoringSystemId = null;
            }
            await _coreSeriesService.Update(model);
        }
    }
}
