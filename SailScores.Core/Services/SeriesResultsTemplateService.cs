using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Api.Enumerations;
using SailScores.Core.Mapping;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Services.Interfaces;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services;

public class SeriesResultsTemplateService : ISeriesResultsTemplateService
{
    private readonly ISailScoresContext _dbContext;
    private readonly IMapper _mapper;

    public SeriesResultsTemplateService(
        ISailScoresContext dbContext,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SeriesResultsTemplate>> GetTemplatesForClubAsync(Guid clubId)
    {
        var dbTemplates = await _dbContext.SeriesResultsTemplates
            .Include(t => t.CustomFields)
            .Include(t => t.ScoreCodes)
            .AsSplitQuery()
            .Where(t => t.ClubId == clubId)
            .OrderBy(t => t.Name)
            .ToListAsync()
            .ConfigureAwait(false);

        return _mapper.Map<List<SeriesResultsTemplate>>(dbTemplates);
    }

    public async Task<SeriesResultsTemplate> GetTemplateAsync(Guid templateId)
    {
        var dbTemplate = await _dbContext.SeriesResultsTemplates
            .Include(t => t.CustomFields)
            .Include(t => t.ScoreCodes)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == templateId)
            .ConfigureAwait(false);

        return _mapper.Map<SeriesResultsTemplate>(dbTemplate);
    }

    // Saves the template's own settings and score code display settings. Custom field selections
    // are saved separately by ICompetitorFieldService.SaveTemplateSelectionsAsync.
    public async Task<SeriesResultsTemplate> SaveTemplateAsync(SeriesResultsTemplate template)
    {
        Db.SeriesResultsTemplate dbTemplate;
        if (template.Id == Guid.Empty)
        {
            dbTemplate = new Db.SeriesResultsTemplate
            {
                Id = Guid.NewGuid(),
                ClubId = template.ClubId
            };
            _dbContext.SeriesResultsTemplates.Add(dbTemplate);
        }
        else
        {
            dbTemplate = await _dbContext.SeriesResultsTemplates
                .Include(t => t.ScoreCodes)
                .FirstOrDefaultAsync(t => t.Id == template.Id)
                .ConfigureAwait(false);

            if (dbTemplate == null || dbTemplate.ClubId != template.ClubId)
            {
                throw new InvalidOperationException("Template not found.");
            }
        }

        CopySettings(template, dbTemplate);
        ReplaceScoreCodes(template, dbTemplate);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        return _mapper.Map<SeriesResultsTemplate>(dbTemplate);
    }

    // Keeps one row per code that isn't shown the default way.
    private void ReplaceScoreCodes(SeriesResultsTemplate source, Db.SeriesResultsTemplate target)
    {
        foreach (var existing in target.ScoreCodes.ToList())
        {
            _dbContext.SeriesResultsTemplateScoreCodes.Remove(existing);
        }
        target.ScoreCodes.Clear();

        var codes = (source.ScoreCodes ?? new List<SeriesResultsTemplateScoreCode>())
            .Where(c => !string.IsNullOrWhiteSpace(c?.Code) && !c.IsDefault)
            .GroupBy(c => c.Code.Trim().ToUpperInvariant())
            .Select(g => new SeriesResultsTemplateScoreCode
            {
                Code = g.Key,
                Display = g.First().Display,
                Faded = g.First().Faded
            });
        // Added through the DbSet (which also fills target.ScoreCodes): a child with its key
        // already set, found through the navigation, would be treated as an existing row.
        foreach (var code in codes)
        {
            _dbContext.SeriesResultsTemplateScoreCodes.Add(
                SeriesResultsTemplateScoreCodeConversions.ToDb(code, target.Id));
        }
    }

    private static void CopySettings(SeriesResultsTemplate source, Db.SeriesResultsTemplate target)
    {
        target.Name = source.Name;
        target.SailNumberVisibility = source.SailNumberVisibility;
        target.CompetitorNameVisibility = source.CompetitorNameVisibility;
        target.CompetitorNameHeader = source.CompetitorNameHeader;
        target.BoatNameVisibility = source.BoatNameVisibility;
        target.BoatNameHeader = source.BoatNameHeader;
        target.CompetitorClubVisibility = source.CompetitorClubVisibility;
        target.PreDiscardTotalVisibility = source.PreDiscardTotalVisibility;
        target.ParticipationVisibility = source.ParticipationVisibility;
        target.ShowPreDiscardTotal = source.PreDiscardTotalVisibility != ColumnVisibility.Hidden;
        target.ShowClubLogo = source.ShowClubLogo;
        target.HighlightTopThree = source.HighlightTopThree;
        target.ColumnSeparators = source.ColumnSeparators;
        target.RaceColumnsVisibility = source.RaceColumnsVisibility;
        target.TotalsSeparator = source.TotalsSeparator;
        target.DiscardStrikethrough = source.DiscardStrikethrough;
        target.DiscardFaded = source.DiscardFaded;
        target.DiscardColor = source.DiscardColor;
    }

    public async Task<IList<Series>> GetPreviewSeriesAsync(Guid clubId, int count)
    {
        var series = await _dbContext.Series
            .Where(s => s.ClubId == clubId
                && _dbContext.HistoricalResults.Any(h => h.SeriesId == s.Id && h.IsCurrent))
            .OrderByDescending(s => s.UpdatedDate)
            .Take(count)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Type,
                s.UpdatedDate,
                SeasonName = s.Season.Name
            })
            .ToListAsync()
            .ConfigureAwait(false);

        return series
            .Select(s => new Series
            {
                Id = s.Id,
                ClubId = clubId,
                Name = s.Name,
                Type = (SeriesType)(int)(s.Type ?? Db.SeriesType.Standard),
                UpdatedDate = s.UpdatedDate,
                Season = new Season { Name = s.SeasonName }
            })
            .ToList();
    }

    public async Task DeleteTemplateAsync(Guid templateId)
    {
        // Check if template is set as a club default
        var isDefault = await _dbContext.Clubs
            .AnyAsync(c => c.DefaultSeriesResultsTemplateId == templateId
                        || c.DefaultRegattaSeriesResultsTemplateId == templateId
                        || c.DefaultSummarySeriesResultsTemplateId == templateId)
            .ConfigureAwait(false);

        if (isDefault)
        {
            throw new InvalidOperationException(
                "Cannot delete this template because it is set as a club default. " +
                "Please change the club default first.");
        }

        var dbTemplate = await _dbContext.SeriesResultsTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId)
            .ConfigureAwait(false);

        if (dbTemplate != null)
        {
            _dbContext.SeriesResultsTemplates.Remove(dbTemplate);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task SetClubDefaultsAsync(
        Guid clubId,
        Guid? seriesTemplateId,
        Guid? regattaTemplateId,
        Guid? summaryTemplateId)
    {
        var club = await _dbContext.Clubs
            .FirstOrDefaultAsync(c => c.Id == clubId)
            .ConfigureAwait(false);

        if (club == null)
        {
            throw new InvalidOperationException("Club not found.");
        }

        var requestedIds = new[] { seriesTemplateId, regattaTemplateId, summaryTemplateId }
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Distinct()
            .ToList();
        var clubTemplateCount = await _dbContext.SeriesResultsTemplates
            .CountAsync(t => t.ClubId == clubId && requestedIds.Contains(t.Id))
            .ConfigureAwait(false);

        if (clubTemplateCount != requestedIds.Count)
        {
            throw new InvalidOperationException("Template not found.");
        }

        club.DefaultSeriesResultsTemplateId = seriesTemplateId;
        club.DefaultRegattaSeriesResultsTemplateId = regattaTemplateId;
        club.DefaultSummarySeriesResultsTemplateId = summaryTemplateId;
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task SeedDefaultTemplatesAsync(Guid clubId)
    {
        // Check if club already has default templates set
        var club = await _dbContext.Clubs
            .FirstOrDefaultAsync(c => c.Id == clubId)
            .ConfigureAwait(false);

        if (club == null)
        {
            throw new InvalidOperationException("Club not found.");
        }

        // Only seed if club doesn't already have templates
        var existingCount = await _dbContext.SeriesResultsTemplates
            .CountAsync(t => t.ClubId == clubId)
            .ConfigureAwait(false);

        if (existingCount > 0)
        {
            return; // Already has templates
        }

        var (preDiscardTotal, participation) = await GetScoringSystemColumnVisibilityAsync(clubId)
            .ConfigureAwait(false);

        // Create Standard template
        var standardTemplate = new Db.SeriesResultsTemplate
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Standard",
            SailNumberVisibility = ColumnVisibility.Always,
            CompetitorNameVisibility = ColumnVisibility.Always,
            CompetitorNameHeader = "Helm",
            BoatNameVisibility = ColumnVisibility.OnLargerScreens,
            BoatNameHeader = "Boat",
            CompetitorClubVisibility = ColumnVisibility.Hidden,
            PreDiscardTotalVisibility = preDiscardTotal,
            ParticipationVisibility = participation,
            ShowPreDiscardTotal = preDiscardTotal != ColumnVisibility.Hidden
        };

        // Create Regatta template
        var regattaTemplate = new Db.SeriesResultsTemplate
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Regatta",
            SailNumberVisibility = ColumnVisibility.Always,
            CompetitorNameVisibility = ColumnVisibility.Always,
            CompetitorNameHeader = "Helm",
            BoatNameVisibility = ColumnVisibility.OnLargerScreens,
            BoatNameHeader = "Boat",
            CompetitorClubVisibility = ColumnVisibility.OnLargerScreens,
            PreDiscardTotalVisibility = preDiscardTotal,
            ParticipationVisibility = participation,
            ShowPreDiscardTotal = preDiscardTotal != ColumnVisibility.Hidden
        };

        _dbContext.SeriesResultsTemplates.Add(standardTemplate);
        _dbContext.SeriesResultsTemplates.Add(regattaTemplate);

        // Set club defaults
        club.DefaultSeriesResultsTemplateId = standardTemplate.Id;
        club.DefaultRegattaSeriesResultsTemplateId = regattaTemplate.Id;

        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    // The standard scoring columns used to be shown by scoring system settings. A club whose
    // scoring systems show one keeps it in its first views: the most visible setting wins.
    // The migration that moved these settings to views does the same for existing views.
    private async Task<(ColumnVisibility PreDiscardTotal, ColumnVisibility Participation)>
        GetScoringSystemColumnVisibilityAsync(Guid clubId)
    {
        var settings = (await _dbContext.ScoringSystems
                .Where(s => s.ClubId == clubId && s.Settings != null)
                .Select(s => s.Settings)
                .ToListAsync()
                .ConfigureAwait(false))
            .Select(ScoringSystemSettings.FromJson)
            .ToList();

        ColumnVisibility MostVisible(string key)
        {
            var shown = settings
                .Select(s => s.GetColumnVisibility(key))
                .Where(v => v is ColumnVisibility.Always or ColumnVisibility.OnLargerScreens)
                .ToList();
            return shown.Contains(ColumnVisibility.Always) ? ColumnVisibility.Always
                : shown.Count > 0 ? ColumnVisibility.OnLargerScreens
                : ColumnVisibility.Hidden;
        }

        return (MostVisible(ScoringColumns.PreDiscardTotalKey), MostVisible(ScoringColumns.ParticipationKey));
    }

    public async Task EnsureDefaultTemplatesForAllClubsAsync()
    {
        var clubsWithoutDefaults = await _dbContext.Clubs
            .Where(c => c.DefaultSeriesResultsTemplateId == null
                     || c.DefaultRegattaSeriesResultsTemplateId == null)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var club in clubsWithoutDefaults)
        {
            await SeedDefaultTemplatesAsync(club.Id).ConfigureAwait(false);
        }
    }
}
