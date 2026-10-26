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
    // A null summary template means summary series use the standard series default.
    Task SetClubDefaultsAsync(Guid clubId, Guid? seriesTemplateId, Guid? regattaTemplateId, Guid? summaryTemplateId);
    Task SeedDefaultTemplatesAsync(Guid clubId);
    // Race dates in results headings; blank uses the club's localization. Throws
    // InvalidOperationException for a format that isn't supported.
    Task SetClubDateFormatAsync(Guid clubId, string dateFormat);
    // The club's most recently updated series with saved results and races (its own, or a
    // child series' for a summary series), for previewing a view.
    // Only the id, name, type, update date and season name are filled.
    Task<IList<Series>> GetPreviewSeriesAsync(Guid clubId, int count);
    Task EnsureDefaultTemplatesForAllClubsAsync();
}
