using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Core.Services.Interfaces;
using SailScores.Database;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services;

public class CompetitorFieldService : ICompetitorFieldService
{
    private readonly ISailScoresContext _dbContext;
    private readonly IMapper _mapper;

    public CompetitorFieldService(ISailScoresContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IList<CompetitorFieldDefinition>> GetFieldDefinitionsAsync(Guid clubId)
    {
        var definitions = await _dbContext.CompetitorFieldDefinitions
            .Where(d => d.ClubId == clubId && d.IsActive)
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.Name)
            .ToListAsync()
            .ConfigureAwait(false);

        return _mapper.Map<List<CompetitorFieldDefinition>>(definitions);
    }

    public async Task<IList<CompetitorFieldDefinition>> GetAllFieldDefinitionsAsync(Guid clubId)
    {
        var definitions = await _dbContext.CompetitorFieldDefinitions
            .Where(d => d.ClubId == clubId)
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.Name)
            .ToListAsync()
            .ConfigureAwait(false);

        return _mapper.Map<List<CompetitorFieldDefinition>>(definitions);
    }

    public async Task<CompetitorFieldDefinition> GetFieldDefinitionAsync(Guid fieldDefinitionId)
    {
        var definition = await _dbContext.CompetitorFieldDefinitions
            .FirstOrDefaultAsync(d => d.Id == fieldDefinitionId)
            .ConfigureAwait(false);

        return _mapper.Map<CompetitorFieldDefinition>(definition);
    }

    public async Task<CompetitorFieldDefinition> SaveFieldDefinitionAsync(CompetitorFieldDefinition definition)
    {
        if (definition.Id == Guid.Empty)
        {
            var dbDefinition = _mapper.Map<Db.CompetitorFieldDefinition>(definition);
            dbDefinition.Id = Guid.NewGuid();
            _dbContext.CompetitorFieldDefinitions.Add(dbDefinition);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return _mapper.Map<CompetitorFieldDefinition>(dbDefinition);
        }

        var existing = await _dbContext.CompetitorFieldDefinitions
            .FirstOrDefaultAsync(d => d.Id == definition.Id)
            .ConfigureAwait(false);

        if (existing == null)
        {
            throw new InvalidOperationException("Custom field definition not found.");
        }

        _mapper.Map(definition, existing);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        return _mapper.Map<CompetitorFieldDefinition>(existing);
    }

    public async Task DeleteFieldDefinitionAsync(Guid fieldDefinitionId)
    {
        var definition = await _dbContext.CompetitorFieldDefinitions
            .FirstOrDefaultAsync(d => d.Id == fieldDefinitionId)
            .ConfigureAwait(false);

        if (definition != null)
        {
            definition.IsActive = false;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task SetFieldActiveStateAsync(Guid fieldDefinitionId, bool isActive)
    {
        var definition = await _dbContext.CompetitorFieldDefinitions
            .FirstOrDefaultAsync(d => d.Id == fieldDefinitionId)
            .ConfigureAwait(false);

        if (definition != null)
        {
            definition.IsActive = isActive;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task DeleteFieldDefinitionPermanentlyAsync(Guid fieldDefinitionId)
    {
        var definition = await _dbContext.CompetitorFieldDefinitions
            .FirstOrDefaultAsync(d => d.Id == fieldDefinitionId)
            .ConfigureAwait(false);

        if (definition != null)
        {
            // Remove any related values first to satisfy FK constraints
            var values = await _dbContext.CompetitorFieldValues
                .Where(v => v.FieldDefinitionId == fieldDefinitionId)
                .ToListAsync()
                .ConfigureAwait(false);
            _dbContext.CompetitorFieldValues.RemoveRange(values);

            _dbContext.CompetitorFieldDefinitions.Remove(definition);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task<IList<CompetitorFieldValue>> GetValuesForCompetitorAsync(Guid competitorId)
    {
        var values = await _dbContext.CompetitorFieldValues
            .Where(v => v.CompetitorId == competitorId)
            .OrderBy(v => v.FieldDefinitionId)
            .ToListAsync()
            .ConfigureAwait(false);

        return _mapper.Map<List<CompetitorFieldValue>>(values);
    }

    public async Task<CompetitorFieldValue> SaveValueAsync(CompetitorFieldValue value)
    {
        if (value.Id == Guid.Empty)
        {
            var dbValue = _mapper.Map<Db.CompetitorFieldValue>(value);
            dbValue.Id = Guid.NewGuid();
            _dbContext.CompetitorFieldValues.Add(dbValue);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return _mapper.Map<CompetitorFieldValue>(dbValue);
        }

        var existing = await _dbContext.CompetitorFieldValues
            .FirstOrDefaultAsync(v => v.Id == value.Id)
            .ConfigureAwait(false);

        if (existing == null)
        {
            throw new InvalidOperationException("Custom field value not found.");
        }

        _mapper.Map(value, existing);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        return _mapper.Map<CompetitorFieldValue>(existing);
    }

    public async Task DeleteValueAsync(Guid valueId)
    {
        var value = await _dbContext.CompetitorFieldValues
            .FirstOrDefaultAsync(v => v.Id == valueId)
            .ConfigureAwait(false);

        if (value != null)
        {
            _dbContext.CompetitorFieldValues.Remove(value);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task<IList<SeriesResultsTemplateCustomField>> GetTemplateSelectionsAsync(Guid templateId)
    {
        var records = await _dbContext.SeriesResultsTemplateCustomFields
            .Where(f => f.SeriesResultsTemplateId == templateId)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.FieldDefinitionId)
            .ToListAsync()
            .ConfigureAwait(false);

        return _mapper.Map<List<SeriesResultsTemplateCustomField>>(records);
    }

    public async Task SaveTemplateSelectionsAsync(Guid templateId, IList<SeriesResultsTemplateCustomField> selections)
    {
        var existing = await _dbContext.SeriesResultsTemplateCustomFields
            .Where(f => f.SeriesResultsTemplateId == templateId)
            .ToListAsync()
            .ConfigureAwait(false);

        _dbContext.SeriesResultsTemplateCustomFields.RemoveRange(existing);

        var orderedSelections = selections
            .Where(s => s != null)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.FieldDefinitionId)
            .ToList();

        var dbSelections = orderedSelections.Select((selection, index) => new Db.SeriesResultsTemplateCustomField
        {
            Id = Guid.NewGuid(),
            SeriesResultsTemplateId = templateId,
            FieldDefinitionId = selection.FieldDefinitionId,
            Visibility = selection.Visibility,
            DisplayOrder = index
        }).ToList();

        await _dbContext.SeriesResultsTemplateCustomFields.AddRangeAsync(dbSelections)
            .ConfigureAwait(false);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
