using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Core.Services.Interfaces;

public interface ISeriesResultsTemplateService
{
    Task<IEnumerable<SeriesResultsTemplate>> GetTemplatesForClubAsync(Guid clubId);
    Task<SeriesResultsTemplate> GetTemplateAsync(Guid templateId);
    Task<SeriesResultsTemplate> SaveTemplateAsync(SeriesResultsTemplate template);
    Task DeleteTemplateAsync(Guid templateId);
    Task SeedDefaultTemplatesAsync(Guid clubId);
    Task EnsureDefaultTemplatesForAllClubsAsync();
}
