using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using ISeriesService = SailScores.Web.Services.Interfaces.ISeriesService;
using Ical.Net;
using Ical.Net.DataTypes;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net.Http;

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
        if (date == default)
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


    public async Task<SeriesWithOptionsViewModel> GetBlankVmForCreate(string clubInitials)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        var club = await _coreClubService.GetMinimalClub(clubId);

        var seasons = await _coreSeasonService.GetSeasons(clubId);

        var vm = new SeriesWithOptionsViewModel
        {
            ClubId = clubId,
            SeasonOptions = seasons
        };
        var selectedSeason = seasons.FirstOrDefault(s =>
            s.Start < DateTime.Now && s.End > DateTime.Now);
        if (selectedSeason == null && seasons.Count() == 1)
        {
            selectedSeason = seasons.First();
        }
        if (selectedSeason != null)
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

    public async Task<MultipleSeriesWithOptionsViewModel> GetBlankVmForCreateMultiple(string clubInitials)
    {
        var blankSingle = await GetBlankVmForCreate(clubInitials);

        return new MultipleSeriesWithOptionsViewModel
        {
            SeasonId = blankSingle.SeasonId,
            SeasonOptions = blankSingle.SeasonOptions,
            ScoringSystemId = blankSingle.ScoringSystemId,
            ScoringSystemOptions = blankSingle.ScoringSystemOptions,
            TrendOption = blankSingle.TrendOption,
            HideDncDiscards = blankSingle.HideDncDiscards,
            Series = new List<MultipleSeriesRowViewModel>
            {
                new()
            }
        };
    }

    public async Task<IList<Guid>> CreateMultipleAsync(
        string clubInitials,
        Guid clubId,
        MultipleSeriesWithOptionsViewModel model,
        string updatedBy)
    {
        var createdIds = new List<Guid>();

        var rows = model.Series
            .Where(r => r != null)
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .ToList();

        // Pre-validate all rows
        var seasons = await _coreSeasonService.GetSeasons(clubId);
        var season = seasons.Single(s => s.Id == model.SeasonId);

        foreach (var row in rows)
        {
            if (row.EnforcedStartDate.HasValue || row.EnforcedEndDate.HasValue)
            {
                var vm = new SeriesWithOptionsViewModel
                {
                    EnforcedStartDate = row.EnforcedStartDate,
                    EnforcedEndDate = row.EnforcedEndDate,
                    DateRestricted = true
                };
                try
                {
                    ValidateDateRestriction(vm, season);
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException($"Series '{row.Name}': {ex.Message}", ex);
                }
            }
        }

        foreach (var row in rows)
        {
            var dateRestricted = row.EnforcedStartDate.HasValue || row.EnforcedEndDate.HasValue;

            var vm = new SeriesWithOptionsViewModel
            {
                ClubId = clubId,
                Name = row.Name.Trim(),
                SeasonId = model.SeasonId,
                ScoringSystemId = model.ScoringSystemId,
                TrendOption = model.TrendOption,
                HideDncDiscards = model.HideDncDiscards,

                Type = SeriesType.Standard,
                ExcludeFromCompetitorStats = false,
                Description = string.Empty,
                IsImportantSeries = false,
                ParentSeriesIds = null,

                DateRestricted = dateRestricted,
                EnforcedStartDate = row.EnforcedStartDate,
                EnforcedEndDate = row.EnforcedEndDate,

                UpdatedBy = updatedBy
            };

            var id = await SaveNew(vm);
            createdIds.Add(id);
        }

        if (model.CreateSummarySeries)
        {
            if (string.IsNullOrWhiteSpace(model.SummarySeriesName))
            {
                throw new InvalidOperationException("To create a summary series, a summary series name is required.");
            }
            if (createdIds.Count == 0)
            {
                throw new InvalidOperationException("No series were created to include in the summary series.");
            }

            var summaryVm = new SeriesWithOptionsViewModel
            {
                ClubId = clubId,
                Name = model.SummarySeriesName.Trim(),
                SeasonId = model.SeasonId,
                Season = season,
                ScoringSystemId = model.ScoringSystemId == Guid.Empty ? null : model.ScoringSystemId,
                TrendOption = model.TrendOption,
                HideDncDiscards = model.HideDncDiscards,

                Type = SeriesType.Summary,
                ExcludeFromCompetitorStats = false,
                Description = string.Empty,
                IsImportantSeries = false,
                ParentSeriesIds = null,

                ChildrenSeriesAsSingleRace = model.SummaryChildrenSeriesAsSingleRace,
                ChildrenSeriesIds = createdIds,

                DateRestricted = false,
                EnforcedStartDate = null,
                EnforcedEndDate = null,

                UpdatedBy = updatedBy
            };

            var summaryId = await _coreSeriesService.SaveNewSeries(summaryVm);
            createdIds.Add(summaryId);
        }

        return createdIds;
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
        foreach (var summaryTypeSeries in coreObject.Where(s => s.Type == SeriesType.Summary))
        {
            var races = coreObject.Where(s => summaryTypeSeries.ChildrenSeriesIds.Contains(s.Id)).SelectMany(s => s.Races).ToList();
            seriesSummaries.Single(ss => ss.Id == summaryTypeSeries.Id).FleetName = GetFleetsString(races);
        }

        return seriesSummaries.OrderByDescending(s => s.Season.Start)
            .ThenBy(s => s.FleetName)
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
            if (model.SeasonId != Guid.Empty)
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
        if (!model.EnforcedStartDate.HasValue || !model.EnforcedEndDate.HasValue)
        {
            throw new InvalidOperationException(ErrorMissingDates);
        }

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

    public async Task<IEnumerable<string>> GetSeriesNamesAsync(Guid clubId, Guid seasonId)
    {
        var allSeries = await _coreSeriesService.GetAllSeriesAsync(clubId, null, true, true);
        return allSeries
            .Where(s => s.Season != null && s.Season.Id == seasonId)
            .Select(s => s.Name)
            .ToList();
    }

    // Does not actually import the calendar events, but prepares them for the CreateMultiple series form.
    public async Task<IcalImportResult> ImportIcalAsync(string clubInitials, Guid seasonId, IFormFile file, string url)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        
        var seasons = await _coreSeasonService.GetSeasons(clubId);
        var season = seasons.FirstOrDefault(s => s.Id == seasonId);
        if (season == null)
        {
            throw new ArgumentException("Invalid season.");
        }

        var icalContent = await GetIcalContentAsync(file, url);
        var calendar = ParseCalendar(icalContent);
        var sortedOccurrences = GetSortedOccurrences(calendar, season);
        
        var (seriesList, outOfRange) = await ProcessOccurrencesAsync(sortedOccurrences, clubId, seasonId, season);

        return new IcalImportResult
        {
            Series = seriesList,
            Warning = outOfRange ? "Some events were outside the season date range and were ignored." : null
        };
    }

    private async Task<string> GetIcalContentAsync(IFormFile file, string url)
    {
        if (file != null && file.Length > 0)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            return await reader.ReadToEndAsync();
        }
        
        if (!string.IsNullOrEmpty(url))
        {
            using var client = new HttpClient();
            return await client.GetStringAsync(url);
        }

        throw new ArgumentException("No file or URL provided.");
    }

    private Ical.Net.Calendar ParseCalendar(string content)
    {
        try
        {
            return Ical.Net.Calendar.Load(content);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to parse iCal content: {ex.Message}");
        }
    }

    private List<Occurrence> GetSortedOccurrences(Ical.Net.Calendar calendar, Season season)
    {
        var start = new CalDateTime(season.Start);
        var end = new CalDateTime(season.End);
        
        var occurrences = new List<Occurrence>();
        foreach (var evt in calendar.Events)
        {
            var eventOccurrences = evt.GetOccurrences(start)
                .TakeWhile(o => o.Period.StartTime.Value <= end.Value);
                
            occurrences.AddRange(eventOccurrences);
        }
        
        return occurrences
            .OrderBy(o => o.Period.StartTime.Value)
            .ToList();
    }

    private async Task<(List<ImportedSeries> Series, bool OutOfRange)> ProcessOccurrencesAsync(
        List<Occurrence> occurrences, 
        Guid clubId, 
        Guid seasonId, 
        Season season)
    {
        var seriesList = new List<ImportedSeries>();
        bool outOfRange = false;

        var existingNames = new HashSet<string>(
            await GetSeriesNamesAsync(clubId, seasonId),
            StringComparer.OrdinalIgnoreCase);
        var currentBatchNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var occurrence in occurrences)
        {
            var evt = occurrence.Source as Ical.Net.CalendarComponents.CalendarEvent;
            if (evt == null) continue;

            var (startDate, endDate) = GetDatesFromOccurrence(occurrence);

            if (!IsWithinSeason(startDate, endDate, season))
            {
                outOfRange = true;
                continue;
            }

            var name = GenerateUniqueName(evt.Summary, startDate, existingNames, currentBatchNames);
            
            currentBatchNames.Add(name);

            seriesList.Add(new ImportedSeries
            {
                Name = name,
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd")
            });
        }

        return (seriesList, outOfRange);
    }

    private (DateOnly Start, DateOnly End) GetDatesFromOccurrence(Occurrence occurrence)
    {
        var evt = occurrence.Source as Ical.Net.CalendarComponents.CalendarEvent;
        var dtStart = occurrence.Period.StartTime.Value;
        var dtEnd = occurrence.Period?.EndTime?.Value ?? dtStart;

        DateOnly startDate;
        DateOnly endDate;

        if (evt != null && evt.IsAllDay)
        {
            startDate = DateOnly.FromDateTime(dtStart);
            endDate = DateOnly.FromDateTime(dtEnd.AddDays(-1));
            if (endDate < startDate) endDate = startDate;
        }
        else
        {
            startDate = DateOnly.FromDateTime(dtStart);
            endDate = DateOnly.FromDateTime(dtEnd);
            if (endDate < startDate) endDate = startDate;
        }
        return (startDate, endDate);
    }

    private bool IsWithinSeason(DateOnly startDate, DateOnly endDate, Season season)
    {
        var seasonStart = DateOnly.FromDateTime(season.Start);
        var seasonEnd = DateOnly.FromDateTime(season.End);

        return !(startDate < seasonStart || endDate > seasonEnd);
    }

    private string GenerateUniqueName(
        string baseName,
        DateOnly startDate,
        HashSet<string> existingNames,
        HashSet<string> currentBatchNames)
    {
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "Untitled Series";
        }
        var name = baseName;

        if (existingNames.Contains(name) || currentBatchNames.Contains(name))
        {
            name = $"{baseName} {startDate:MMM dd}";
            if (existingNames.Contains(name) || currentBatchNames.Contains(name))
            {
                int counter = 1;
                var nameWithDate = name;
                do
                {
                    name = $"{nameWithDate} {counter}";
                    counter++;
                } while (existingNames.Contains(name) || currentBatchNames.Contains(name));
            }
        }
        return name;
    }
}
