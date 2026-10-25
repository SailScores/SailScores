using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using Xunit;
using Db = SailScores.Database.Entities;

namespace SailScores.Test.Unit.Core.Services;

public class SeriesResultsTemplateServiceTests
{
    private readonly ISailScoresContext _context;
    private readonly SeriesResultsTemplateService _service;
    private readonly Guid _clubId;
    private readonly Guid _otherClubId;
    private readonly Guid _otherClubTemplateId;

    public SeriesResultsTemplateServiceTests()
    {
        _context = InMemoryContextBuilder.GetContext();
        _service = new SeriesResultsTemplateService(_context, MapperBuilder.GetSailScoresMapper());
        _clubId = _context.Clubs.First().Id;

        var otherClub = new Db.Club { Id = Guid.NewGuid(), Name = "Other Club", Initials = "OTHER" };
        _context.Clubs.Add(otherClub);
        _otherClubId = otherClub.Id;
        _otherClubTemplateId = Guid.NewGuid();
        _context.SeriesResultsTemplates.Add(new Db.SeriesResultsTemplate
        {
            Id = _otherClubTemplateId,
            ClubId = otherClub.Id,
            Name = "Other"
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task SetClubDefaultsAsync_WithClubTemplates_SetsAllDefaults()
    {
        await _service.SeedDefaultTemplatesAsync(_clubId);
        var custom = await _service.SaveTemplateAsync(
            new SeriesResultsTemplate { ClubId = _clubId, Name = "Custom" });
        var standardId = _context.Clubs.Single(c => c.Id == _clubId).DefaultSeriesResultsTemplateId;

        await _service.SetClubDefaultsAsync(_clubId, custom.Id, standardId, custom.Id);

        var club = _context.Clubs.Single(c => c.Id == _clubId);
        Assert.Equal(custom.Id, club.DefaultSeriesResultsTemplateId);
        Assert.Equal(standardId, club.DefaultRegattaSeriesResultsTemplateId);
        Assert.Equal(custom.Id, club.DefaultSummarySeriesResultsTemplateId);
    }

    [Fact]
    public async Task SetClubDefaultsAsync_WithNulls_ClearsDefaults()
    {
        await _service.SeedDefaultTemplatesAsync(_clubId);

        await _service.SetClubDefaultsAsync(_clubId, null, null, null);

        var club = _context.Clubs.Single(c => c.Id == _clubId);
        Assert.Null(club.DefaultSeriesResultsTemplateId);
        Assert.Null(club.DefaultRegattaSeriesResultsTemplateId);
        Assert.Null(club.DefaultSummarySeriesResultsTemplateId);
    }

    [Fact]
    public async Task SetClubDefaultsAsync_WithAnotherClubsTemplate_ThrowsAndLeavesDefaults()
    {
        await _service.SeedDefaultTemplatesAsync(_clubId);
        var before = _context.Clubs.Single(c => c.Id == _clubId).DefaultSeriesResultsTemplateId;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SetClubDefaultsAsync(_clubId, _otherClubTemplateId, null, null));

        Assert.Equal(before, _context.Clubs.Single(c => c.Id == _clubId).DefaultSeriesResultsTemplateId);
    }

    [Fact]
    public async Task SaveTemplateAsync_UpdatingAnotherClubsTemplate_Throws()
    {
        var template = new SeriesResultsTemplate { Id = _otherClubTemplateId, ClubId = _clubId, Name = "Taken" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveTemplateAsync(template));

        var saved = _context.SeriesResultsTemplates.Single(t => t.Id == _otherClubTemplateId);
        Assert.Equal(_otherClubId, saved.ClubId);
        Assert.Equal("Other", saved.Name);
    }

    [Fact]
    public async Task SaveTemplateAsync_NewTemplate_SavesDisplayOptions()
    {
        var saved = await _service.SaveTemplateAsync(new SeriesResultsTemplate
        {
            ClubId = _clubId,
            Name = "Sparse",
            HighlightTopThree = false,
            ColumnSeparators = ResultsColumnSeparator.RaceDays,
            RaceColumnsVisibility = ColumnVisibility.OnLargerScreens
        });

        var dbTemplate = _context.SeriesResultsTemplates.Single(t => t.Id == saved.Id);
        Assert.Equal(_clubId, dbTemplate.ClubId);
        Assert.False(dbTemplate.HighlightTopThree);
        Assert.Equal(ResultsColumnSeparator.RaceDays, dbTemplate.ColumnSeparators);
        Assert.Equal(ColumnVisibility.OnLargerScreens, dbTemplate.RaceColumnsVisibility);
        Assert.False(saved.HighlightTopThree);
    }

    [Fact]
    public async Task SaveTemplateAsync_ExistingTemplate_UpdatesSettingsAndKeepsCustomFields()
    {
        var created = await _service.SaveTemplateAsync(new SeriesResultsTemplate { ClubId = _clubId, Name = "View" });
        var fieldDefinition = new Db.CompetitorFieldDefinition
        {
            Id = Guid.NewGuid(),
            ClubId = _clubId,
            Name = "Crew"
        };
        _context.CompetitorFieldDefinitions.Add(fieldDefinition);
        _context.SeriesResultsTemplateCustomFields.Add(new Db.SeriesResultsTemplateCustomField
        {
            Id = Guid.NewGuid(),
            SeriesResultsTemplateId = created.Id,
            FieldDefinitionId = fieldDefinition.Id
        });
        await _context.SaveChangesAsync();

        await _service.SaveTemplateAsync(new SeriesResultsTemplate
        {
            Id = created.Id,
            ClubId = _clubId,
            Name = "Renamed",
            ColumnSeparators = ResultsColumnSeparator.EveryColumn
        });

        var dbTemplate = _context.SeriesResultsTemplates.Single(t => t.Id == created.Id);
        Assert.Equal("Renamed", dbTemplate.Name);
        Assert.Equal(ResultsColumnSeparator.EveryColumn, dbTemplate.ColumnSeparators);
        Assert.Single(_context.SeriesResultsTemplateCustomFields.Where(f => f.SeriesResultsTemplateId == created.Id));
    }

    [Fact]
    public async Task SaveTemplateAsync_WithScoreCodes_KeepsOnlyNonDefaultCodes()
    {
        var saved = await _service.SaveTemplateAsync(new SeriesResultsTemplate
        {
            ClubId = _clubId,
            Name = "Sparse",
            ScoreCodes = new List<SeriesResultsTemplateScoreCode>
            {
                new() { Code = "dnc", Display = ScoreCodeDisplay.Hidden },
                new() { Code = "DNF", Display = ScoreCodeDisplay.CodeOnly, Faded = true },
                new() { Code = "OCS", Display = ScoreCodeDisplay.CodeAndPoints }
            }
        });

        var codes = _context.SeriesResultsTemplateScoreCodes
            .Where(c => c.SeriesResultsTemplateId == saved.Id)
            .OrderBy(c => c.Code)
            .ToList();
        Assert.Equal(new[] { "DNC", "DNF" }, codes.Select(c => c.Code));
        Assert.Equal(ScoreCodeDisplay.Hidden, codes[0].Display);
        Assert.True(codes[1].Faded);
        Assert.Equal(2, saved.ScoreCodes.Count);
    }

    [Fact]
    public async Task SaveTemplateAsync_ExistingScoreCodes_ReplacesThem()
    {
        var created = await _service.SaveTemplateAsync(new SeriesResultsTemplate
        {
            ClubId = _clubId,
            Name = "Sparse",
            ScoreCodes = new List<SeriesResultsTemplateScoreCode>
            {
                new() { Code = "DNC", Display = ScoreCodeDisplay.Hidden }
            }
        });

        await _service.SaveTemplateAsync(new SeriesResultsTemplate
        {
            Id = created.Id,
            ClubId = _clubId,
            Name = "Sparse",
            ScoreCodes = new List<SeriesResultsTemplateScoreCode>
            {
                new() { Code = "DNF", Faded = true }
            }
        });

        var reloaded = await _service.GetTemplateAsync(created.Id);
        var code = Assert.Single(reloaded.ScoreCodes);
        Assert.Equal("DNF", code.Code);
        Assert.True(code.Faded);
    }

    [Fact]
    public async Task SetClubDefaultsAsync_SummaryFromAnotherClub_Throws()
    {
        await _service.SeedDefaultTemplatesAsync(_clubId);
        var standardId = _context.Clubs.Single(c => c.Id == _clubId).DefaultSeriesResultsTemplateId;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SetClubDefaultsAsync(_clubId, standardId, standardId, _otherClubTemplateId));

        Assert.Null(_context.Clubs.Single(c => c.Id == _clubId).DefaultSummarySeriesResultsTemplateId);
    }

    [Fact]
    public async Task DeleteTemplateAsync_SummaryDefault_Throws()
    {
        var summary = await _service.SaveTemplateAsync(
            new SeriesResultsTemplate { ClubId = _clubId, Name = "Summary" });
        await _service.SetClubDefaultsAsync(_clubId, null, null, summary.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteTemplateAsync(summary.Id));

        Assert.True(_context.SeriesResultsTemplates.Any(t => t.Id == summary.Id));
    }

    [Fact]
    public async Task SaveTemplateAsync_DiscardSettings_SavesThem()
    {
        var saved = await _service.SaveTemplateAsync(new SeriesResultsTemplate
        {
            ClubId = _clubId,
            Name = "Red discards",
            DiscardStrikethrough = false,
            DiscardFaded = false,
            DiscardColor = ResultsTextColor.Red
        });

        var db = _context.SeriesResultsTemplates.Single(t => t.Id == saved.Id);
        Assert.False(db.DiscardStrikethrough);
        Assert.False(db.DiscardFaded);
        Assert.Equal(ResultsTextColor.Red, db.DiscardColor);
    }

    [Theory]
    [InlineData(ColumnVisibility.OnLargerScreens, true)]
    [InlineData(ColumnVisibility.Hidden, false)]
    public async Task SaveTemplateAsync_ScoringColumns_SavesThemAndKeepsOldFlagInStep(
        ColumnVisibility preDiscardTotal, bool expectedShowPreDiscardTotal)
    {
        var saved = await _service.SaveTemplateAsync(new SeriesResultsTemplate
        {
            ClubId = _clubId,
            Name = "Columns",
            PreDiscardTotalVisibility = preDiscardTotal,
            ParticipationVisibility = ColumnVisibility.Always,
            TotalsSeparator = true
        });

        var db = _context.SeriesResultsTemplates.Single(t => t.Id == saved.Id);
        Assert.Equal(preDiscardTotal, db.PreDiscardTotalVisibility);
        Assert.Equal(ColumnVisibility.Always, db.ParticipationVisibility);
        Assert.True(db.TotalsSeparator);
        // The previous release reads ShowPreDiscardTotal.
        Assert.Equal(expectedShowPreDiscardTotal, db.ShowPreDiscardTotal);
    }

    [Fact]
    public async Task SeedDefaultTemplatesAsync_ScoringSystemsShowStandardColumns_ViewsShowThem()
    {
        _context.SeriesResultsTemplates.RemoveRange(_context.SeriesResultsTemplates.Where(t => t.ClubId == _clubId));
        _context.ScoringSystems.Add(new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            ClubId = _clubId,
            Name = "Shows participation",
            Settings = "{\"ColumnVisibilities\":{\"participation\":2}}"
        });
        _context.ScoringSystems.Add(new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            ClubId = _clubId,
            Name = "Shows both",
            Settings = "{\"ColumnVisibilities\":{\"participation\":1,\"preDiscardTotal\":2}}"
        });
        _context.ScoringSystems.Add(new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            ClubId = _otherClubId,
            Name = "Other club",
            Settings = "{\"ColumnVisibilities\":{\"preDiscardTotal\":1}}"
        });
        _context.SaveChanges();

        await _service.SeedDefaultTemplatesAsync(_clubId);

        var templates = _context.SeriesResultsTemplates.Where(t => t.ClubId == _clubId).ToList();
        Assert.Equal(2, templates.Count);
        Assert.All(templates, t =>
        {
            Assert.Equal(ColumnVisibility.Always, t.ParticipationVisibility);
            Assert.Equal(ColumnVisibility.OnLargerScreens, t.PreDiscardTotalVisibility);
        });
    }

    [Fact]
    public async Task SeedDefaultTemplatesAsync_NoScoringSystemSettings_HidesStandardColumns()
    {
        _context.SeriesResultsTemplates.RemoveRange(_context.SeriesResultsTemplates.Where(t => t.ClubId == _clubId));
        _context.SaveChanges();

        await _service.SeedDefaultTemplatesAsync(_clubId);

        Assert.All(_context.SeriesResultsTemplates.Where(t => t.ClubId == _clubId), t =>
        {
            Assert.Equal(ColumnVisibility.Hidden, t.ParticipationVisibility);
            Assert.Equal(ColumnVisibility.Hidden, t.PreDiscardTotalVisibility);
        });
    }

    [Fact]
    public async Task GetPreviewSeriesAsync_ClubSeries_OnlyWithCurrentResultsNewestFirst()
    {
        var season = _context.Seasons.First(s => s.ClubId == _clubId);
        Db.Series AddSeries(string name, Guid clubId, DateTime updated, bool? currentResults)
        {
            var series = new Db.Series
            {
                Id = Guid.NewGuid(),
                ClubId = clubId,
                Name = name,
                Season = season,
                UpdatedDate = updated,
                Type = Db.SeriesType.Summary
            };
            _context.Series.Add(series);
            if (currentResults.HasValue)
            {
                _context.HistoricalResults.Add(new Db.HistoricalResults
                {
                    Id = Guid.NewGuid(),
                    SeriesId = series.Id,
                    IsCurrent = currentResults.Value,
                    Results = "{}"
                });
            }
            return series;
        }
        var older = AddSeries("Older", _clubId, new DateTime(2026, 5, 1), true);
        var newer = AddSeries("Newer", _clubId, new DateTime(2026, 6, 1), true);
        AddSeries("Old results only", _clubId, new DateTime(2026, 7, 1), false);
        AddSeries("No results", _clubId, new DateTime(2026, 7, 1), null);
        AddSeries("Other club", _otherClubId, new DateTime(2026, 7, 1), true);
        _context.SaveChanges();

        var result = await _service.GetPreviewSeriesAsync(_clubId, 10);

        Assert.Equal(new[] { newer.Id, older.Id }, result.Select(s => s.Id));
        Assert.Equal(season.Name, result[0].Season.Name);
        Assert.Equal(SeriesType.Summary, result[0].Type);
    }
}
