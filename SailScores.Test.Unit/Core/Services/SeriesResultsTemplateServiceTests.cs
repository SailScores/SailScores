using System;
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
    public async Task SetClubDefaultsAsync_WithClubTemplates_SetsBothDefaults()
    {
        await _service.SeedDefaultTemplatesAsync(_clubId);
        var custom = await _service.SaveTemplateAsync(
            new SeriesResultsTemplate { ClubId = _clubId, Name = "Custom" });
        var standardId = _context.Clubs.Single(c => c.Id == _clubId).DefaultSeriesResultsTemplateId;

        await _service.SetClubDefaultsAsync(_clubId, custom.Id, standardId);

        var club = _context.Clubs.Single(c => c.Id == _clubId);
        Assert.Equal(custom.Id, club.DefaultSeriesResultsTemplateId);
        Assert.Equal(standardId, club.DefaultRegattaSeriesResultsTemplateId);
    }

    [Fact]
    public async Task SetClubDefaultsAsync_WithNulls_ClearsDefaults()
    {
        await _service.SeedDefaultTemplatesAsync(_clubId);

        await _service.SetClubDefaultsAsync(_clubId, null, null);

        var club = _context.Clubs.Single(c => c.Id == _clubId);
        Assert.Null(club.DefaultSeriesResultsTemplateId);
        Assert.Null(club.DefaultRegattaSeriesResultsTemplateId);
    }

    [Fact]
    public async Task SetClubDefaultsAsync_WithAnotherClubsTemplate_ThrowsAndLeavesDefaults()
    {
        await _service.SeedDefaultTemplatesAsync(_clubId);
        var before = _context.Clubs.Single(c => c.Id == _clubId).DefaultSeriesResultsTemplateId;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SetClubDefaultsAsync(_clubId, _otherClubTemplateId, null));

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
}
