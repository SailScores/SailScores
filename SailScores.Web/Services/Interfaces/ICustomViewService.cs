using SailScores.Core.Model;

namespace SailScores.Web.Services.Interfaces;

/// <summary>
/// Service for handling custom view-related lookups and data preparation.
/// </summary>
public interface ICustomViewService
{
    /// <summary>
    /// Gets the display header for a custom field in a template.
    /// </summary>
    string GetCustomFieldHeader(
        SeriesResultsTemplateCustomField templateField,
        CompetitorFieldDefinition fieldDefinition);

    /// <summary>
    /// Gets the value of a custom field on a competitor that is effective on a date, falling
    /// back to an undated value. When no date is supplied, the current UTC date is used, and
    /// if nothing is effective today the most recent value is returned.
    /// </summary>
    string GetCustomFieldValue(Competitor competitor, Guid fieldDefinitionId, DateTime? effectiveDate = null);

    /// <summary>
    /// Gets custom field display configuration for a template,
    /// filtered by visibility and ordered for display.
    /// </summary>
    IList<SeriesResultsTemplateCustomField> GetVisibleCustomFields(SeriesResultsTemplate template);

    /// <summary>
    /// Gets the regatta results view template for a club.
    /// Returns the club's default regatta series results template.
    /// </summary>
    Task<SeriesResultsTemplate> GetRegattaResultsViewTemplateAsync(Guid clubId);
}
