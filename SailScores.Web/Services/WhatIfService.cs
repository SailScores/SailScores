using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Database.Entities;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class WhatIfService : IWhatIfService
{
    private readonly IScoringService _coreScoringService;
    private readonly Core.Services.ISeriesService _coreSeriesService;
    private readonly IMapper _mapper;

    public WhatIfService(
        Core.Services.IScoringService scoringService,
        Core.Services.ISeriesService seriesService,
        IMapper mapper)
    {
        _coreScoringService = scoringService;
        _coreSeriesService = seriesService;
        _mapper = mapper;
    }

    public async Task<IList<Core.Model.ScoringSystem>> GetScoringSystemOptions(Guid clubId)
    {
        var scoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(clubId, true);
        scoringSystemOptions.Add(new Core.Model.ScoringSystem
        {
            Id = Guid.Empty,
            Name = "<Use Club Default>"
        });
        return scoringSystemOptions.OrderBy(s => s.Name).ToList();
    }

    public async Task<WhatIfResultsViewModel> GetResults(WhatIfViewModel options)
    {
        Guid seriesId = options.SeriesId ?? options.Series?.Id ?? default;

        var series = await _coreSeriesService.GetOneSeriesAsync(seriesId);
        var newSeries = await _coreSeriesService.CalculateWhatIfScoresAsync(seriesId,
            options.SelectedScoringSystemId ?? default,
            options.Discards, 
            options.ParticipationPercent);
        

        var vm = new WhatIfResultsViewModel
        {
            SeriesId = seriesId,
            Series = series,
            AlternateResults = newSeries.FlatResults,
        };
        return vm;
    }
}