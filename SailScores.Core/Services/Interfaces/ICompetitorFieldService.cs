using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services.Interfaces;

public interface ICompetitorFieldService
{
    Task<IList<CompetitorFieldDefinition>> GetFieldDefinitionsAsync(Guid clubId);

    Task<CompetitorFieldDefinition> GetFieldDefinitionAsync(Guid fieldDefinitionId);

    Task<CompetitorFieldDefinition> SaveFieldDefinitionAsync(CompetitorFieldDefinition definition);

    Task DeleteFieldDefinitionAsync(Guid fieldDefinitionId);
    Task SetFieldActiveStateAsync(Guid fieldDefinitionId, bool isActive);
    Task DeleteFieldDefinitionPermanentlyAsync(Guid fieldDefinitionId);

    Task<IList<CompetitorFieldValue>> GetValuesForCompetitorAsync(Guid competitorId);

    Task<CompetitorFieldValue> SaveValueAsync(CompetitorFieldValue value);

    Task DeleteValueAsync(Guid valueId);

    Task<IList<SeriesResultsTemplateCustomField>> GetTemplateSelectionsAsync(Guid templateId);

    Task SaveTemplateSelectionsAsync(Guid templateId, IList<SeriesResultsTemplateCustomField> selections);

    // Return all field definitions for a club including inactive ones (admin use)
    Task<IList<CompetitorFieldDefinition>> GetAllFieldDefinitionsAsync(Guid clubId);
}
