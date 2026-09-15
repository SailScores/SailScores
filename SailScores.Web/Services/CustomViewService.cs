using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Core.Services.Interfaces;
using SailScores.Database;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

/// <summary>
/// Service for handling custom view-related lookups and data preparation,
/// such as resolving custom field headers and values for exports and displays.
/// </summary>
public class CustomViewService : ICustomViewService
{
    private readonly ISailScoresContext _dbContext;
    private readonly ISeriesResultsTemplateService _templateService;

    public CustomViewService(
        ISailScoresContext dbContext,
        ISeriesResultsTemplateService templateService)
    {
        _dbContext = dbContext;
        _templateService = templateService;
    }
    /// <summary>
    /// Gets the display header for a custom field in a template.
    /// Uses the field definition name as the default header.
    /// </summary>
    public string GetCustomFieldHeader(
        SeriesResultsTemplateCustomField templateField,
        CompetitorFieldDefinition fieldDefinition)
    {
        if (templateField == null || fieldDefinition == null)
        {
            return "Unknown Field";
        }

        // For now, we use the field definition name as the display header.
        // In the future, this could be enhanced to support custom headers per template.
        return fieldDefinition.Name;
    }

    /// <summary>
    /// Gets the current or most recent value for a custom field on a competitor,
    /// respecting effective date ranges.
    /// </summary>
    public string GetCustomFieldValue(
        Competitor competitor,
        Guid fieldDefinitionId)
    {
        if (competitor?.CustomFieldValues == null)
        {
            return null;
        }

        var fieldValues = competitor.CustomFieldValues
            .Where(v => v.FieldDefinitionId == fieldDefinitionId)
            .Where(v => !string.IsNullOrWhiteSpace(v.Value))
            .ToList();

        if (!fieldValues.Any())
        {
            return null;
        }

        var today = DateTime.UtcNow.Date;

        // Try to find a value that is currently effective
        var currentValue = fieldValues
            .Where(v => (!v.EffectiveFrom.HasValue || v.EffectiveFrom.Value.Date <= today)
                && (!v.EffectiveTo.HasValue || v.EffectiveTo.Value.Date >= today))
            .FirstOrDefault();

        if (currentValue != null)
        {
            return currentValue.Value;
        }

        // Fall back to undated value (EffectiveFrom and EffectiveTo both null)
        var undatedValue = fieldValues
            .Where(v => !v.EffectiveFrom.HasValue && !v.EffectiveTo.HasValue)
            .FirstOrDefault();

        if (undatedValue != null)
        {
            return undatedValue.Value;
        }

        // Fall back to most recent by any criteria
        return fieldValues.FirstOrDefault()?.Value;
    }

    /// <summary>
    /// Gets custom field display configuration for a template.
    /// Returns an ordered list of template fields filtered by visibility.
    /// </summary>
    public IList<SeriesResultsTemplateCustomField> GetVisibleCustomFields(SeriesResultsTemplate template)
    {
        if (template?.CustomFields == null)
        {
            return new List<SeriesResultsTemplateCustomField>();
        }

        return template.CustomFields
            .Where(c => c.Visibility != SailScores.Api.Enumerations.ColumnVisibility.Hidden)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.FieldDefinitionId)
            .ToList();
    }

    /// <summary>
    /// Gets the regatta results view template for a club.
    /// Returns the club's default regatta series results template.
    /// </summary>
    public async Task<SeriesResultsTemplate> GetRegattaResultsViewTemplateAsync(Guid clubId)
    {
        var club = await _dbContext.Clubs
            .Where(c => c.Id == clubId)
            .Select(c => new { c.DefaultRegattaSeriesResultsTemplateId })
            .FirstOrDefaultAsync();

        if (club?.DefaultRegattaSeriesResultsTemplateId == null)
        {
            return null;
        }

        return await _templateService.GetTemplateAsync(club.DefaultRegattaSeriesResultsTemplateId.Value);
    }
}
