using AutoMapper;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class SeriesService : ISeriesService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly Core.Services.ISeriesService _coreSeriesService;
        private readonly IScoringService _coreScoringService;
        private readonly Core.Services.ISeasonService _coreSeasonService;
        private readonly IMapper _mapper;

        public SeriesService(
            Core.Services.IClubService clubService,
            Core.Services.ISeriesService seriesService,
            Core.Services.IScoringService scoringService,
            Core.Services.ISeasonService seasonService,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreSeriesService = seriesService;
            _coreScoringService = scoringService;
            _coreSeasonService = seasonService;
            _mapper = mapper;
        }

        public async Task DeleteAsync(Guid id)
        {
            await _coreSeriesService.Delete(id);
        }

        public async Task<SeriesWithOptionsViewModel> GetBlankVmForCreate(string clubInitials)
        {
            var clubId = await _coreClubService.GetClubId(clubInitials);

            var seasons = await _coreSeasonService.GetSeasons(clubId);

            var vm = new SeriesWithOptionsViewModel
            {
                SeasonOptions = seasons
            };
            var selectedSeason = seasons.FirstOrDefault(s =>
                s.Start < DateTime.Now && s.End > DateTime.Now);
            if(selectedSeason == null && seasons.Count() == 1)
            {
                selectedSeason = seasons.First();
            }
            if(selectedSeason != null)
            {
                vm.SeasonId = selectedSeason.Id;
            }
            var scoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(clubId, true);
            scoringSystemOptions.Add(new ScoringSystem
            {
                Id = Guid.Empty,
                Name = "<Use Club Default>"
            });
            vm.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();

            return vm;

        }

        public async Task<FlatChartData> GetChartData(Guid seriesId)
        {
            return await _coreSeriesService.GetChartData(seriesId);
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
            var series = await _coreSeriesService.GetSeriesDetailsAsync(clubInitials, season, seriesUrlName);

            return series;
        }

        public async Task<Series> GetSeriesAsync(Guid seriesId)
        {
            var series = await _coreSeriesService.GetOneSeriesAsync(seriesId);

            return series;
        }

        public async Task SaveNew(SeriesWithOptionsViewModel model)
        {
            var seasons = await _coreSeasonService.GetSeasons(model.ClubId);
            var season = seasons.Single(s => s.Id == model.SeasonId);
            model.Season = season;
            if (model.ScoringSystemId == Guid.Empty)
            {
                model.ScoringSystemId = null;
            }
            await _coreSeriesService.SaveNewSeries(model);
        }

        public async Task Update(SeriesWithOptionsViewModel model)
        {
            var seasons = await _coreSeasonService.GetSeasons(model.ClubId);
            var season = seasons.Single(s => s.Id == model.SeasonId);
            model.Season = season;
            if (model.ScoringSystemId == Guid.Empty)
            {
                model.ScoringSystemId = null;
            }
            await _coreSeriesService.Update(model);
        }
    }
}
