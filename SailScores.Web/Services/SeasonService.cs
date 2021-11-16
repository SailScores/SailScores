using SailScores.Core.Model;
using ISeasonService = SailScores.Web.Services.Interfaces.ISeasonService;

namespace SailScores.Web.Services;

public class SeasonService : ISeasonService
{
    private readonly Core.Services.ISeasonService _coreSeasonService;
    private readonly IMapper _mapper;

    public SeasonService(
        Core.Services.ISeasonService seasonService,
        IMapper mapper)
    {
        _coreSeasonService = seasonService;
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

    public async Task<Season> GetSeasonSuggestion(Guid clubId)
    {
        var existingSeasons = await _coreSeasonService.GetSeasons(clubId);

        if ((existingSeasons?.Count() ?? 0) == 0)
        {
            var today = DateTime.Today;
            return new Season
            {
                ClubId = clubId,
                Name = today.Year.ToString(CultureInfo.InvariantCulture),
                Start = new DateTime(today.Year, 1, 1),
                End = new DateTime(today.Year, 12, 31)
            };
        }

        var lastSeason = existingSeasons
            .OrderByDescending(s => s.Start)
            .First();

        var season = new Season()
        {
            Start = lastSeason.Start.AddYears(1),
            End = lastSeason.End.AddYears(1)
        };

        if (!existingSeasons.Any(s =>
            s.Name == season.Start.Year.ToString(CultureInfo.InvariantCulture)))
        {
            season.Name = season.Start.Year.ToString(CultureInfo.InvariantCulture);
        }

        return season;
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