using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using SailScores.Core.Mapping;
using SailScores.Core.Services;
using SailScores.Core.Services.Interfaces;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using Xunit;

namespace SailScores.Test.Unit.Core.Services;

public class CompetitorFieldServiceAdditionalTests
{
    private readonly ISailScoresContext _context;
    private readonly ICompetitorFieldService _service;

    public CompetitorFieldServiceAdditionalTests()
    {
        _context = InMemoryContextBuilder.GetContext();
        var config = new MapperConfiguration(opts => { opts.AddProfile(new DbToModelMappingProfile()); });
        var mapper = config.CreateMapper();
        _service = new CompetitorFieldService(_context, mapper);
    }

    [Fact]
    public async Task SetFieldActiveStateAsync_TogglesActiveState()
    {
        var clubId = _context.Clubs.First().Id;
        var def = new Database.Entities.CompetitorFieldDefinition
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "TempField",
            DisplayHeader = "Temp",
            DataType = Database.Entities.CustomFieldDataType.Text,
            DisplayOrder = 1,
            IsActive = true
        };
        _context.CompetitorFieldDefinitions.Add(def);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.SetFieldActiveStateAsync(def.Id, false);

        var saved = _context.CompetitorFieldDefinitions.Single(d => d.Id == def.Id);
        Assert.False(saved.IsActive);

        await _service.SetFieldActiveStateAsync(def.Id, true);
        saved = _context.CompetitorFieldDefinitions.Single(d => d.Id == def.Id);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task DeleteFieldDefinitionPermanentlyAsync_RemovesDefinitionAndValues()
    {
        var clubId = _context.Clubs.First().Id;
        var def = new Database.Entities.CompetitorFieldDefinition
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "ToDelete",
            DisplayHeader = "ToDelete",
            DataType = Database.Entities.CustomFieldDataType.Text,
            DisplayOrder = 1,
            IsActive = true
        };
        _context.CompetitorFieldDefinitions.Add(def);

        var value = new Database.Entities.CompetitorFieldValue
        {
            Id = Guid.NewGuid(),
            CompetitorId = _context.Competitors.First().Id,
            FieldDefinitionId = def.Id,
            Value = "X",
            EffectiveFrom = null,
            EffectiveTo = null
        };
        _context.CompetitorFieldValues.Add(value);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteFieldDefinitionPermanentlyAsync(def.Id);

        Assert.Empty(_context.CompetitorFieldDefinitions.Where(d => d.Id == def.Id));
        Assert.Empty(_context.CompetitorFieldValues.Where(v => v.FieldDefinitionId == def.Id));
    }
}
