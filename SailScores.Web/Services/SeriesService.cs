using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using ISeriesService = SailScores.Web.Services.Interfaces.ISeriesService;

namespace SailScores.Web.Services;

public class SeriesService : ISeriesService
{
    private readonly Core.Services.IClubService _coreClubService;
    private readonly Core.Services.ISeriesService _coreSeriesService;
    private readonly IScoringService _coreScoringService;
    private readonly Core.Services.ISeasonService _coreSeasonService;
    private readonly IMapper _mapper;
    
    // Validation error messages for date restrictions
    private const string ErrorMissingDates = "Both start date and end date must be provided when date restriction is enabled.";
    private const string ErrorStartDateOutOfRange = "Series start date must fall within the selected season.";
    private const string ErrorEndDateOutOfRange = "Series end date must fall within the selected season.";
    private const string ErrorStartAfterEnd = "Series start date must be before or equal to the end date.";

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

    public async Task<IEnumerable<SeriesSummary>> GetSummarySeriesAsync(Guid clubId)
    {
        var coreObject = await _coreSeriesService.GetAllSeriesAsync(clubId, null, false, true);
        var summarySeries = coreObject
            .Where(s => s.Type == SeriesType.Summary);

        var seriesSummaries = _mapper.Map<IList<SeriesSummary>>(summarySeries);

        return seriesSummaries.OrderByDescending(s => s.Season.Start)
            .ThenBy(s => s.Name);
    }


    public async Task<IEnumerable<SeriesSummary>> GetSummarySeriesAsync(
        Guid clubId,
        DateTime date)
    {
        if(date == default)
        {
            return await GetSummarySeriesAsync(clubId);
        }

        var coreObject = await _coreSeriesService.GetAllSeriesAsync(clubId, date, false, true);
        var summarySeries = coreObject
            .Where(s => s.Type == SeriesType.Summary);

        var seriesSummaries = _mapper.Map<IList<SeriesSummary>>(summarySeries);

        return seriesSummaries.OrderByDescending(s => s.Season.Start)
            .ThenBy(s => s.Name);
    }


    public async Task<SeriesWithOptionsViewModel> UpdateVmOptions(
        string clubInitials,
        SeriesWithOptionsViewModel partialSeries)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        var club = await _coreClubService.GetMinimalClub(clubId);

        var allSeasons = await _coreSeasonService.GetSeasons(clubId);
        var seasons = allSeasons;

        if (partialSeries.SeasonId == default)
        {
            seasons = await _coreSeasonService.GetSeasons(clubId);
            var selectedSeason = seasons.FirstOrDefault(s =>
                s.Start < DateTime.Now && s.End > DateTime.Now);
            if (selectedSeason == null && seasons.Count() == 1)
            {
                selectedSeason = seasons.First();
            }
            if (selectedSeason != null)
            {
                partialSeries.SeasonId = selectedSeason.Id;
            }

        } else
        {
            seasons = allSeasons
                .Where(s => s.Id == partialSeries.SeasonId)
                .ToList();
        }

        partialSeries.SeasonOptions = seasons;


        var scoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(clubId, false);
        scoringSystemOptions.Add(new ScoringSystem
        {
            Id = Guid.Empty,
            Name = "<Use Club Default>"
        });
        partialSeries.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();

        // Get summary series options
        var selectedSeasonForSummary = seasons.FirstOrDefault(s => s.Id == partialSeries.SeasonId);
        var centerDate = selectedSeasonForSummary != null
            ? selectedSeasonForSummary.Start.AddDays(
                (selectedSeasonForSummary.End - selectedSeasonForSummary.Start).TotalDays / 2)
            : default;
        partialSeries.SummarySeriesOptions = (await GetSummarySeriesAsync(clubId, centerDate)).ToList();

        return partialSeries;

    }

    public async Task<SeriesWithOptionsViewModel> GetBlankVmForCreate(string clubInitials)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        var club = await _coreClubService.GetMinimalClub(clubId);

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
        var scoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(clubId, false);
        scoringSystemOptions.Add(new ScoringSystem
        {
            Id = Guid.Empty,
            Name = "<Use Club Default>"
        });
        vm.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();

        // Get summary series options
        vm.SummarySeriesOptions = (await GetSummarySeriesAsync(clubId)).ToList();

        return vm;

    }

    public async Task<FlatChartData> GetChartData(Guid seriesId)
    {
        return await _coreSeriesService.GetChartData(seriesId);
    }

    public async Task<IEnumerable<SeriesSummary>> GetNonRegattaSeriesSummariesAsync(string clubInitials)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        var coreObject = await _coreSeriesService.GetAllSeriesAsync(clubId, null, false, true);

        var seriesSummaries = _mapper.Map<IList<SeriesSummary>>(coreObject);
        foreach(var summaryTypeSeries in coreObject.Where(s => s.Type == SeriesType.Summary))
        {
            var races = coreObject.Where(s => summaryTypeSeries.ChildrenSeriesIds.Contains(s.Id)).SelectMany(s => s.Races).ToList();
            seriesSummaries.Single(ss => ss.Id == summaryTypeSeries.Id).FleetName = GetFleetsString(races);
        }

        return seriesSummaries.OrderByDescending(s => s.Season.Start)
            .ThenBy( s => s.FleetName)
            .ThenBy(s => s.Name);
    }


    private static String GetFleetsString(IList<Race> races)
    {
        var fleetNames = races.Select(r => r.Fleet).Where(f => f != null)
            .Select(f => f.Name).Distinct();
        switch (fleetNames.Count())
        {
            case 0:
                return "No Fleet";
            case 1:
                return fleetNames.First();
            default:
                return "Multiple Fleets";
        }
    }

    public async Task<IEnumerable<SeriesSummary>> GetChildSeriesSummariesAsync(
        Guid clubId,
        Guid seasonId)
    {
        var coreObject = await _coreSeriesService.GetAllSeriesAsync(clubId, null, false, false);
        var eligibleSeries = coreObject
            .Where(s => (s.Type == default ? SeriesType.Standard : s.Type) == SeriesType.Standard
                && s.Season.Id == seasonId);

        var seriesSummaries = _mapper.Map<IList<SeriesSummary>>(eligibleSeries);

        return seriesSummaries.OrderByDescending(s => s.Season.Start)
            .ThenBy(s => s.FleetName)
            .ThenBy(s => s.Name);
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

    public async Task<Guid> SaveNew(SeriesWithOptionsViewModel model)
    {
        var seasons = await _coreSeasonService.GetSeasons(model.ClubId);
        var season = seasons.Single(s => s.Id == model.SeasonId);
        model.Season = season;
        if (model.ScoringSystemId == Guid.Empty)
        {
            model.ScoringSystemId = null;
        }
        
        // Handle date restriction logic
        if (model.DateRestricted == true)
        {
            ValidateDateRestriction(model, season);
        }
        else
        {
            // Clear dates when not restricted
            model.EnforcedStartDate = null;
            model.EnforcedEndDate = null;
        }
        
        return await _coreSeriesService.SaveNewSeries(model);
    }

    public async Task Update(SeriesWithOptionsViewModel model)
    {
        // no longer allowing update of season.
        if (model.ScoringSystemId == Guid.Empty)
        {
            model.ScoringSystemId = null;
        }
        
        // Handle date restriction logic
        if (model.DateRestricted == true)
        {
            Season season;
            
            // Check if SeasonId is available in the model (not default/empty)
            if (model.SeasonId != Guid.Empty && model.SeasonId != default)
            {
                // Use the season from the model
                var seasons = await _coreSeasonService.GetSeasons(model.ClubId);
                season = seasons.Single(s => s.Id == model.SeasonId);
            }
            else
            {
                // Get season from database since the season input is disabled on Edit page
                var existingSeries = await _coreSeriesService.GetOneSeriesAsync(model.Id);
                if (existingSeries?.Season == null)
                {
                    throw new InvalidOperationException("Could not retrieve season for series validation.");
                }
                season = existingSeries.Season;
            }
            
            ValidateDateRestriction(model, season);
        }
        else
        {
            // Clear dates when not restricted
            model.EnforcedStartDate = null;
            model.EnforcedEndDate = null;
        }
        
        await _coreSeriesService.Update(model);
    }
    
    private void ValidateDateRestriction(SeriesWithOptionsViewModel model, Season season)
    {
        // When date restriction is enabled, both dates must be provided
        if (!model.EnforcedStartDate.HasValue || !model.EnforcedEndDate.HasValue)
        {
            throw new InvalidOperationException(ErrorMissingDates);
        }
        
        // Convert Season DateTime to DateOnly for comparison
        var seasonStart = DateOnly.FromDateTime(season.Start);
        var seasonEnd = DateOnly.FromDateTime(season.End);
        
        // Validate start date falls within season
        if (model.EnforcedStartDate.Value < seasonStart || model.EnforcedStartDate.Value > seasonEnd)
        {
            throw new InvalidOperationException(ErrorStartDateOutOfRange);
        }
        
        // Validate end date falls within season
        if (model.EnforcedEndDate.Value < seasonStart || model.EnforcedEndDate.Value > seasonEnd)
        {
            throw new InvalidOperationException(ErrorEndDateOutOfRange);
        }
        
        // Validate start is before end
        if (model.EnforcedStartDate.Value > model.EnforcedEndDate.Value)
        {
            throw new InvalidOperationException(ErrorStartAfterEnd);
        }
    }
}
