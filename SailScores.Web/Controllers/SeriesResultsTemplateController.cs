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

namespace SailScores.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
public class SeriesResultsTemplateController : Controller
{
    private readonly ISeriesResultsTemplateService _templateService;
    private readonly IClubService _clubService;
    private readonly ICompetitorFieldService _competitorFieldService;
    private readonly IMapper _mapper;

    public SeriesResultsTemplateController(
        ISeriesResultsTemplateService templateService,
        IClubService clubService,
        ICompetitorFieldService competitorFieldService,
        IMapper mapper)
    {
        _templateService = templateService;
        _clubService = clubService;
        _competitorFieldService = competitorFieldService;
        _mapper = mapper;
    }

    // GET: SeriesResultsTemplate
    public async Task<ActionResult> Index(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;
        var clubId = await _clubService.GetClubId(clubInitials);
        var templates = await _templateService.GetTemplatesForClubAsync(clubId);

        var vm = new SeriesResultsTemplateListViewModel
        {
            ClubInitials = clubInitials,
            Templates = templates.ToList()
        };

        return View(vm);
    }

    // GET: SeriesResultsTemplate/Create
    public async Task<ActionResult> Create(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;
        var clubId = await _clubService.GetClubId(clubInitials);

        var vm = new SeriesResultsTemplateViewModel
        {
            ClubId = clubId,
            ClubInitials = clubInitials,
            AvailableCustomFields = (await _competitorFieldService.GetFieldDefinitionsAsync(clubId)).ToList()
        };

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
            if (!ModelState.IsValid)
            {
                model.ClubInitials = clubInitials;
                return View(model);
            }

            var clubId = await _clubService.GetClubId(clubInitials);
            model.ClubId = clubId;

            var template = _mapper.Map<SeriesResultsTemplate>(model);
            var savedTemplate = await _templateService.SaveTemplateAsync(template);
            await _competitorFieldService.SaveTemplateSelectionsAsync(savedTemplate.Id, model.CustomFieldSelections
                .Where(s => s.Selected)
                .Select(s => new SeriesResultsTemplateCustomField
                {
                    FieldDefinitionId = s.FieldDefinitionId,
                    Visibility = s.Visibility,
                    DisplayOrder = s.DisplayOrder
                })
                .ToList());

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
    public async Task<ActionResult> Edit(string clubInitials, Guid id)
    {
        ViewData["ClubInitials"] = clubInitials;
        var template = await _templateService.GetTemplateAsync(id);

        if (template == null)
        {
            return NotFound();
        }

        var vm = _mapper.Map<SeriesResultsTemplateViewModel>(template);
        vm.ClubInitials = clubInitials;
        vm.AvailableCustomFields = (await _competitorFieldService.GetFieldDefinitionsAsync(await _clubService.GetClubId(clubInitials))).ToList();
        var selections = await _competitorFieldService.GetTemplateSelectionsAsync(template.Id);
        var selectionLookup = selections.ToDictionary(s => s.FieldDefinitionId, s => s);
        vm.CustomFieldSelections = vm.AvailableCustomFields
            .Select(field => new TemplateCustomFieldSelectionViewModel
            {
                FieldDefinitionId = field.Id,
                Name = field.Name,
                DisplayHeader = field.DisplayHeader,
                DataType = field.DataType,
                Selected = selectionLookup.ContainsKey(field.Id),
                Visibility = selectionLookup.TryGetValue(field.Id, out var value) ? value.Visibility : ColumnVisibility.Hidden,
                DisplayOrder = selectionLookup.TryGetValue(field.Id, out var value2) ? value2.DisplayOrder : 0
            })
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.DisplayHeader)
            .ToList();

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
            if (!ModelState.IsValid)
            {
                model.ClubInitials = clubInitials;
                return View(model);
            }

            model.Id = id;
            var clubId = await _clubService.GetClubId(clubInitials);
            model.ClubId = clubId;

            var template = _mapper.Map<SeriesResultsTemplate>(model);
            var savedTemplate = await _templateService.SaveTemplateAsync(template);
            await _competitorFieldService.SaveTemplateSelectionsAsync(savedTemplate.Id, model.CustomFieldSelections
                .Where(s => s.Selected)
                .Select(s => new SeriesResultsTemplateCustomField
                {
                    FieldDefinitionId = s.FieldDefinitionId,
                    Visibility = s.Visibility,
                    DisplayOrder = s.DisplayOrder
                })
                .ToList());

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

    // GET: SeriesResultsTemplate/Delete/5
    public async Task<ActionResult> Delete(string clubInitials, Guid id)
    {
        ViewData["ClubInitials"] = clubInitials;
        var template = await _templateService.GetTemplateAsync(id);

        if (template == null)
        {
            return NotFound();
        }

        var clubId = await _clubService.GetClubId(clubInitials);
        var club = await _clubService.GetMinimalClub(clubId);

        var vm = _mapper.Map<SeriesResultsTemplateViewModel>(template);
        vm.ClubInitials = clubInitials;
        vm.IsClubDefault = template.Id == club.DefaultSeriesResultsTemplateId 
                        || template.Id == club.DefaultRegattaSeriesResultsTemplateId;

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
            var club = await _clubService.GetMinimalClub(clubId);

            // Check if template is a club default
            if (id == club.DefaultSeriesResultsTemplateId || id == club.DefaultRegattaSeriesResultsTemplateId)
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
}
