using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using ISeasonService = SailScores.Web.Services.Interfaces.ISeasonService;

namespace SailScores.Web.Services;

public class SeasonService : ISeasonService
{
    private readonly Core.Services.ISeasonService _coreSeasonService;
    private readonly Core.Services.IScoringService _coreScoringService;
    private readonly IMapper _mapper;

    public SeasonService(
        Core.Services.ISeasonService seasonService,
        Core.Services.IScoringService scoringService,
        IMapper mapper)
    {
        _coreSeasonService = seasonService;
        _coreScoringService = scoringService;
        _mapper = mapper;
    }

    public async Task Delete(Guid seasonId)
    {
        await _coreSeasonService.Delete(seasonId);
    }

    public async Task<IList<string>> GetSavingSeasonErrors(Season model)
    {
        return await _coreSeasonService.GetSavingSeasonErrors(model);
    }

    public async Task<SeasonWithOptionsViewModel> GetSeasonSuggestion(Guid clubId)
    {
        var existingSeasons = await _coreSeasonService.GetSeasons(clubId);

        var today = DateTime.Today;
        var vm = new SeasonWithOptionsViewModel
        {
            ClubId = clubId,
            Name = today.Year.ToString(CultureInfo.InvariantCulture),
            Start = new DateTime(today.Year, 1, 1),
            End = new DateTime(today.Year, 12, 31)
        };

        if (existingSeasons?.Any() == true)
        {
            var lastSeason = existingSeasons
                .OrderByDescending(s => s.Start)
                .First();

            vm.Start = lastSeason.Start.AddYears(1);
            vm.End = lastSeason.End.AddYears(1);

            if (!existingSeasons.Any(s =>
                s.Name == vm.Start.Year.ToString(CultureInfo.InvariantCulture)))
            {
                vm.Name = vm.Start.Year.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                vm.Name = string.Empty;
            }
        }

        vm.ScoringSystemOptions = await GetScoringSystemOptionsAsync(clubId);

        return vm;
    }

    public async Task<SeasonWithOptionsViewModel> GetSeasonForEdit(Guid clubId, Guid seasonId)
    {
        var season = (await _coreSeasonService.GetSeasons(clubId))
            .SingleOrDefault(s => s.Id == seasonId);
        
        if (season == null)
        {
            return null;
        }

        var vm = new SeasonWithOptionsViewModel
        {
            Id = season.Id,
            ClubId = clubId,
            Name = season.Name,
            Start = season.Start,
            End = season.End,
            DefaultScoringSystemId = season.DefaultScoringSystemId,
            ScoringSystemOptions = await GetScoringSystemOptionsAsync(clubId)
        };

        return vm;
    }

    private async Task<IList<ScoringSystem>> GetScoringSystemOptionsAsync(Guid clubId)
    {
        var scoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(clubId, false);
        scoringSystemOptions.Add(new ScoringSystem
        {
            Id = Guid.Empty,
            Name = "<Club Default>"
        });
        return scoringSystemOptions.OrderBy(s => s.Name).ToList();
    }

    public async Task<IList<Season>> GetSeasons(Guid clubId)
    {
        return await _coreSeasonService.GetSeasons(clubId);
    }

    public async Task SaveNew(Season season)
    {
        await _coreSeasonService.SaveNew(season);
    }

    public async Task Update(Season season)
    {
        await _coreSeasonService.Update(season);
    }
}
