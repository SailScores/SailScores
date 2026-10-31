using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Core.Services.Interfaces;
using SailScores.Web.Authorization;
using SailScores.Web.Models.SailScores;
using IWebClubService = SailScores.Web.Services.Interfaces.IClubService;

namespace SailScores.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
public class SeriesResultsTemplateController : Controller
{
    private readonly ISeriesResultsTemplateService _templateService;
    private readonly IClubService _clubService;
    private readonly ICompetitorFieldService _competitorFieldService;
    private readonly ISeriesService _seriesService;
    private readonly IWebClubService _webClubService;

    // How many recent series the preview offers.
    private const int PreviewSeriesCount = 30;

    public SeriesResultsTemplateController(
        ISeriesResultsTemplateService templateService,
        IClubService clubService,
        ICompetitorFieldService competitorFieldService,
        ISeriesService seriesService,
        IWebClubService webClubService)
    {
        _templateService = templateService;
        _clubService = clubService;
        _competitorFieldService = competitorFieldService;
        _seriesService = seriesService;
        _webClubService = webClubService;
    }

    // GET: SeriesResultsTemplate
    public async Task<ActionResult> Index(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;
        var clubId = await _clubService.GetClubId(clubInitials);
        var club = await _clubService.GetMinimalClub(clubId);

        if (club.DefaultSeriesResultsTemplateId == null || club.DefaultRegattaSeriesResultsTemplateId == null)
        {
            await _templateService.SeedDefaultTemplatesAsync(clubId);
            club = await _clubService.GetMinimalClub(clubId);
        }

        var templates = await _templateService.GetTemplatesForClubAsync(clubId);

        var vm = new SeriesResultsTemplateListViewModel
        {
            ClubInitials = clubInitials,
            Templates = templates.ToList(),
            DefaultSeriesResultsTemplateId = club.DefaultSeriesResultsTemplateId,
            DefaultRegattaSeriesResultsTemplateId = club.DefaultRegattaSeriesResultsTemplateId,
            DefaultSummarySeriesResultsTemplateId = club.DefaultSummarySeriesResultsTemplateId,
            DefaultDateFormat = club.DefaultDateFormat
        };

        return View(vm);
    }

    // POST: SeriesResultsTemplate/SetDefaults
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SetDefaults(string clubInitials, SeriesResultsTemplateListViewModel model)
    {
        // A template id that doesn't bind would otherwise clear that default.
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "The default views could not be saved.";
            return RedirectToAction(nameof(Index), new { clubInitials });
        }
        var clubId = await _clubService.GetClubId(clubInitials);
        try
        {
            await _templateService.SetClubDefaultsAsync(
                clubId,
                model.DefaultSeriesResultsTemplateId,
                model.DefaultRegattaSeriesResultsTemplateId,
                model.DefaultSummarySeriesResultsTemplateId);
            TempData["SuccessMessage"] = "Default views saved.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { clubInitials });
    }

    // POST: SeriesResultsTemplate/SetDateFormat
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> SetDateFormat(string clubInitials, string defaultDateFormat)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        try
        {
            await _templateService.SetClubDateFormatAsync(clubId, defaultDateFormat);
            // Results pages read the format from the cached club.
            _webClubService.InvalidateClubCache(clubInitials);
            TempData["SuccessMessage"] = "Date format saved.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { clubInitials });
    }

    // GET: SeriesResultsTemplate/Create
    public async Task<ActionResult> Create(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;
        var clubId = await _clubService.GetClubId(clubInitials);

        var vm = new SeriesResultsTemplateViewModel
        {
            ClubId = clubId,
            ClubInitials = clubInitials
        };
        await LoadCustomFieldsAsync(vm, clubId, templateId: null);
        await LoadScoreCodesAsync(vm, clubId);
        await LoadPreviewSeriesAsync(vm, clubId);

        return View(vm);
    }

    // POST: SeriesResultsTemplate/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(string clubInitials, SeriesResultsTemplateViewModel model)
    {
        ViewData["ClubInitials"] = clubInitials;
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!ModelState.IsValid)
            {
                model.ClubInitials = clubInitials;
                await LoadFieldDefinitionsAsync(model, clubId);
                await LoadScoreCodesAsync(model, clubId);
                await LoadPreviewSeriesAsync(model, clubId);
                return View(model);
            }

            model.ClubId = clubId;

            var template = model.ToModel();
            var savedTemplate = await _templateService.SaveTemplateAsync(template);
            await _competitorFieldService.SaveTemplateSelectionsAsync(
                savedTemplate.Id, GetSelectedCustomFields(model));

            TempData["SuccessMessage"] = $"Template '{model.Name}' created successfully.";
            return RedirectToAction(nameof(Index), new { clubInitials });
        }
        catch (Exception ex)
        {
            model.ClubInitials = clubInitials;
            ModelState.AddModelError(string.Empty, $"Error creating template: {ex.Message}");
            return View(model);
        }
    }

    // GET: SeriesResultsTemplate/Edit/5
    // With reset, the form shows the default settings, keeping the name; nothing is saved
    // until the form is.
    public async Task<ActionResult> Edit(string clubInitials, Guid id, bool reset = false)
    {
        ViewData["ClubInitials"] = clubInitials;
        var clubId = await _clubService.GetClubId(clubInitials);
        var template = await GetClubTemplateAsync(clubId, id);

        if (template == null)
        {
            return NotFound();
        }

        var vm = reset
            ? new SeriesResultsTemplateViewModel
            {
                Id = template.Id,
                ClubId = template.ClubId,
                Name = template.Name,
                IsReset = true
            }
            : new SeriesResultsTemplateViewModel(template);
        vm.ClubInitials = clubInitials;
        await LoadCustomFieldsAsync(vm, clubId, reset ? null : template.Id);
        await LoadScoreCodesAsync(vm, clubId);
        await LoadPreviewSeriesAsync(vm, clubId);

        return View(vm);
    }

    // POST: SeriesResultsTemplate/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(string clubInitials, Guid id, SeriesResultsTemplateViewModel model)
    {
        ViewData["ClubInitials"] = clubInitials;
        try
        {
            model.Id = id;
            var clubId = await _clubService.GetClubId(clubInitials);
            if (await GetClubTemplateAsync(clubId, id) == null)
            {
                return NotFound();
            }
            if (!ModelState.IsValid)
            {
                model.ClubInitials = clubInitials;
                await LoadFieldDefinitionsAsync(model, clubId);
                await LoadScoreCodesAsync(model, clubId);
                await LoadPreviewSeriesAsync(model, clubId);
                return View(model);
            }
            model.ClubId = clubId;

            var template = model.ToModel();
            var savedTemplate = await _templateService.SaveTemplateAsync(template);
            await _competitorFieldService.SaveTemplateSelectionsAsync(
                savedTemplate.Id, GetSelectedCustomFields(model));

            TempData["SuccessMessage"] = $"Template '{model.Name}' updated successfully.";
            return RedirectToAction(nameof(Index), new { clubInitials });
        }
        catch (Exception ex)
        {
            model.ClubInitials = clubInitials;
            ModelState.AddModelError(string.Empty, $"Error updating template: {ex.Message}");
            return View(model);
        }
    }

    // POST: SeriesResultsTemplate/Preview
    // A club series' results table shown with the form's settings, which aren't saved.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Preview(
        string clubInitials,
        Guid? previewSeriesId,
        SeriesResultsTemplateViewModel model)
    {
        // The preview doesn't show the name, so a form still missing one can be previewed.
        ModelState.Remove(nameof(SeriesResultsTemplateViewModel.Name));
        if (!ModelState.IsValid)
        {
            return PartialView("_Preview", null);
        }
        var clubId = await _clubService.GetClubId(clubInitials);
        var seriesId = previewSeriesId
            ?? (await _templateService.GetPreviewSeriesAsync(clubId, 1)).FirstOrDefault()?.Id;
        var series = seriesId.HasValue ? await _seriesService.GetOneSeriesAsync(seriesId.Value) : null;
        if (series == null || series.ClubId != clubId || series.FlatResults == null)
        {
            return PartialView("_Preview", null);
        }

        var template = model.ToModel();
        template.ClubId = clubId;
        template.CustomFields = GetSelectedCustomFields(model);
        var club = await _clubService.GetMinimalClub(clubId);

        var table = new ResultsTableViewModel
        {
            SeriesId = series.Id,
            Results = series.FlatResults,
            CustomFieldColumns = template.CustomFields,
            CustomFieldDefinitions = (await GetFieldDefinitionsAsync(clubId)).ToDictionary(d => d.Id, d => d),
            ShowExportButtons = false,
            ExcludeCompetitorLinks = true,
            PreferAlternativeSailNumbers = series.PreferAlternativeSailNumbers ?? false,
            UpdatedDate = series.UpdatedDate,
            DefaultDateFormat = club?.DefaultDateFormat
        }.WithTemplate(SeriesResultsTemplateHelper.GetResolvedTemplate(template));

        return PartialView("_Preview", table);
    }

    // GET: SeriesResultsTemplate/Delete/5
    public async Task<ActionResult> Delete(string clubInitials, Guid id)
    {
        ViewData["ClubInitials"] = clubInitials;
        var clubId = await _clubService.GetClubId(clubInitials);
        var template = await GetClubTemplateAsync(clubId, id);

        if (template == null)
        {
            return NotFound();
        }

        var club = await _clubService.GetMinimalClub(clubId);

        var vm = new SeriesResultsTemplateViewModel(template);
        vm.ClubInitials = clubInitials;
        vm.IsClubDefault = IsClubDefault(club, template.Id);

        return View(vm);
    }

    // POST: SeriesResultsTemplate/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteConfirmed(string clubInitials, Guid id)
    {
        ViewData["ClubInitials"] = clubInitials;
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (await GetClubTemplateAsync(clubId, id) == null)
            {
                return NotFound();
            }
            var club = await _clubService.GetMinimalClub(clubId);

            // Check if template is a club default
            if (IsClubDefault(club, id))
            {
                TempData["ErrorMessage"] = "Cannot delete a template that is set as a club default. Please change the club defaults first.";
                return RedirectToAction(nameof(Delete), new { clubInitials, id });
            }

            await _templateService.DeleteTemplateAsync(id);

            TempData["SuccessMessage"] = "Template deleted successfully.";
            return RedirectToAction(nameof(Index), new { clubInitials });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Delete), new { clubInitials, id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting template: {ex.Message}";
            return RedirectToAction(nameof(Delete), new { clubInitials, id });
        }
    }

    private async Task<IList<CompetitorFieldDefinition>> GetFieldDefinitionsAsync(Guid clubId) =>
        (await _competitorFieldService.GetFieldDefinitionsAsync(clubId)).ToList();

    // Fills the custom field choices: every club field, marked with the template's selections.
    private async Task LoadFieldDefinitionsAsync(SeriesResultsTemplateViewModel vm, Guid clubId)
    {
        var club = await _clubService.GetMinimalClub(clubId);
        vm.CustomFieldsEnabled = club?.EnableCustomCompetitorFields ?? false;
        vm.AlternativeSailNumbersEnabled = club?.EnableAlternativeSailNumbers ?? false;
        vm.AvailableCustomFields = await GetFieldDefinitionsAsync(clubId);
    }

    private async Task LoadCustomFieldsAsync(SeriesResultsTemplateViewModel vm, Guid clubId, Guid? templateId)
    {
        await LoadFieldDefinitionsAsync(vm, clubId);
        var selections = templateId.HasValue
            ? await _competitorFieldService.GetTemplateSelectionsAsync(templateId.Value)
            : new List<SeriesResultsTemplateCustomField>();
        var selectionLookup = selections.ToDictionary(s => s.FieldDefinitionId, s => s);
        vm.CustomFieldSelections = vm.AvailableCustomFields
            .Select(field =>
            {
                var isSelected = selectionLookup.TryGetValue(field.Id, out var selection);
                return new TemplateCustomFieldSelectionViewModel
                {
                    FieldDefinitionId = field.Id,
                    Name = field.Name,
                    DisplayHeader = field.DisplayHeader,
                    DataType = field.DataType,
                    Selected = isSelected,
                    Visibility = isSelected ? selection.Visibility : ColumnVisibility.Hidden,
                    DisplayOrder = isSelected ? selection.DisplayOrder : 0
                };
            })
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.DisplayHeader)
            .ToList();
    }

    // One row per score code the club has set up or used, plus any codes the template already
    // has settings for, keeping the template's settings.
    private async Task LoadScoreCodesAsync(SeriesResultsTemplateViewModel vm, Guid clubId)
    {
        var clubCodes = (await _templateService.GetClubScoreCodesAsync(clubId))
            .ToDictionary(c => c.Name, c => c.Description, StringComparer.OrdinalIgnoreCase);
        var saved = (vm.ScoreCodes ?? new List<TemplateScoreCodeViewModel>())
            .Where(c => !string.IsNullOrWhiteSpace(c.Code))
            .GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        vm.ScoreCodes = clubCodes.Keys
            .Union(saved.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(code =>
            {
                var row = saved.TryGetValue(code, out var existing)
                    ? existing
                    : new TemplateScoreCodeViewModel { Code = code };
                row.Description = clubCodes.TryGetValue(code, out var description) ? description : null;
                return row;
            })
            .OrderBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task LoadPreviewSeriesAsync(SeriesResultsTemplateViewModel vm, Guid clubId)
    {
        vm.PreviewSeries = await _templateService.GetPreviewSeriesAsync(clubId, PreviewSeriesCount);
    }

    private static IList<SeriesResultsTemplateCustomField> GetSelectedCustomFields(
        SeriesResultsTemplateViewModel model) =>
        (model.CustomFieldSelections ?? new List<TemplateCustomFieldSelectionViewModel>())
            .Where(s => s.Selected)
            .Select(s => new SeriesResultsTemplateCustomField
            {
                FieldDefinitionId = s.FieldDefinitionId,
                Visibility = s.Visibility,
                DisplayOrder = s.DisplayOrder
            })
            .ToList();

    private static bool IsClubDefault(Club club, Guid templateId) =>
        templateId == club.DefaultSeriesResultsTemplateId
        || templateId == club.DefaultRegattaSeriesResultsTemplateId
        || templateId == club.DefaultSummarySeriesResultsTemplateId;

    // The template, or null when it doesn't exist or belongs to another club.
    private async Task<SeriesResultsTemplate> GetClubTemplateAsync(Guid clubId, Guid templateId)
    {
        var template = await _templateService.GetTemplateAsync(templateId);
        return template?.ClubId == clubId ? template : null;
    }
}
