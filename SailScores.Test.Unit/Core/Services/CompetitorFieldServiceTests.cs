using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using SailScores.Core.Mapping;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Core.Services.Interfaces;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using Xunit;

namespace SailScores.Test.Unit.Core.Services;

public class CompetitorFieldServiceTests
{
    private readonly ISailScoresContext _context;
    private readonly IMapper _mapper;
    private readonly ICompetitorFieldService _service;

    public CompetitorFieldServiceTests()
    {
        _context = InMemoryContextBuilder.GetContext();
        var config = new MapperConfiguration(opts =>
        {
            opts.AddProfile(new DbToModelMappingProfile());
        });
        _mapper = config.CreateMapper();
        _service = new CompetitorFieldService(_context, _mapper);
    }

    [Fact]
    public async Task SaveFieldDefinitionAsync_PersistsDefinitionAndReturnsMappedModel()
    {
        var clubId = _context.Clubs.First().Id;
        var definition = new CompetitorFieldDefinition
        {
            ClubId = clubId,
            Name = "Boat Type",
            DisplayHeader = "Boat Type",
            DataType = CustomFieldDataType.Text,
            DisplayOrder = 3,
            IsActive = true,
            HighlyVisible = true
        };

        var result = await _service.SaveFieldDefinitionAsync(definition);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Boat Type", result.Name);
        Assert.Equal(clubId, result.ClubId);
        Assert.True(result.HighlyVisible);

        var savedDefinition = _context.CompetitorFieldDefinitions.Single(d => d.ClubId == clubId);
        Assert.True(savedDefinition.HighlyVisible);
        Assert.Single(_context.CompetitorFieldDefinitions.Where(d => d.ClubId == clubId));
    }

    [Fact]
    public async Task SaveTemplateSelectionsAsync_ReplacesExistingSelectionsAndPreservesOrder()
    {
        var clubId = _context.Clubs.First().Id;
        var template = new Database.Entities.SeriesResultsTemplate
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Template"
        };
        _context.SeriesResultsTemplates.Add(template);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var firstField = new Database.Entities.CompetitorFieldDefinition
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "One",
            DisplayHeader = "One",
            DataType = Database.Entities.CustomFieldDataType.Text,
            DisplayOrder = 1
        };
        var secondField = new Database.Entities.CompetitorFieldDefinition
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Two",
            DisplayHeader = "Two",
            DataType = Database.Entities.CustomFieldDataType.Text,
            DisplayOrder = 2
        };
        _context.CompetitorFieldDefinitions.Add(firstField);
        _context.CompetitorFieldDefinitions.Add(secondField);
        _context.SeriesResultsTemplateCustomFields.Add(new Database.Entities.SeriesResultsTemplateCustomField
        {
            Id = Guid.NewGuid(),
            SeriesResultsTemplateId = template.Id,
            FieldDefinitionId = firstField.Id,
            Visibility = SailScores.Api.Enumerations.ColumnVisibility.Hidden,
            DisplayOrder = 0
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var selections = new List<SeriesResultsTemplateCustomField>
        {
            new SeriesResultsTemplateCustomField
            {
                FieldDefinitionId = secondField.Id,
                Visibility = SailScores.Api.Enumerations.ColumnVisibility.Always,
                DisplayOrder = 1
            },
            new SeriesResultsTemplateCustomField
            {
                FieldDefinitionId = firstField.Id,
                Visibility = SailScores.Api.Enumerations.ColumnVisibility.Always,
                DisplayOrder = 0
            }
        };

        await _service.SaveTemplateSelectionsAsync(template.Id, selections);

        var savedSelections = _context.SeriesResultsTemplateCustomFields
            .Where(f => f.SeriesResultsTemplateId == template.Id)
            .OrderBy(f => f.DisplayOrder)
            .ToList();

        Assert.Equal(2, savedSelections.Count);
        Assert.Equal(firstField.Id, savedSelections[0].FieldDefinitionId);
        Assert.Equal(secondField.Id, savedSelections[1].FieldDefinitionId);
        Assert.Equal(SailScores.Api.Enumerations.ColumnVisibility.Always, savedSelections[0].Visibility);
    }
}
