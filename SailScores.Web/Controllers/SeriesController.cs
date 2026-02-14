using System.Text;
using System.Web;
using System.IO;
using System.Net.Http;
using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Services;
using SailScores.Identity.Entities;
using SailScores.Web.Authorization;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;
using ISeriesService = SailScores.Web.Services.Interfaces.ISeriesService;

namespace SailScores.Web.Controllers;

public class SeriesController : Controller
{

    private readonly ISeriesService _seriesService;
    private readonly Core.Services.IClubService _clubService;
    private readonly IAuthorizationService _authService;
    private readonly IAdminTipService _adminTipService;
    private readonly ICsvService _csvService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly IForwarderService _forwarderService;

    public SeriesController(
        ISeriesService seriesService,
        Core.Services.IClubService clubService,
        IAuthorizationService authService,
        IAdminTipService adminTipService,
        ICsvService csvService,
        IForwarderService forwarderService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _seriesService = seriesService;
        _clubService = clubService;
        _authService = authService;
        _adminTipService = adminTipService;
        _csvService = csvService;
        _forwarderService = forwarderService;
        _userManager = userManager;
        _mapper = mapper;
    }

    // GET: Series
    [ResponseCache(Duration = 900)]
    public async Task<ActionResult> Index(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials.ToUpperInvariant();

        var series = await _seriesService.GetNonRegattaSeriesSummariesAsync(clubInitials);
        var clubName = await _clubService.GetClubName(clubInitials);
        var clubId = await _clubService.GetClubId(clubInitials);

        return View(new ClubCollectionViewModel<SeriesSummary>
        {
            List = series,
            ClubInitials = clubInitials,
            ClubName = clubName,
            CanEdit = await _authService.CanUserEdit(User, clubId),
            CanEditSeries = await _authService.CanUserEditSeries(User, clubId)
        });
    }

    public async Task<ActionResult> Details(
        string clubInitials,
        string season,
        string seriesName)
    {
        ViewData["ClubInitials"] = clubInitials;

        var series = await _seriesService.GetSeriesAsync(clubInitials, season, seriesName);
        if (series == null)
        {
            var forward = await _forwarderService.GetSeriesForwarding(clubInitials, season, seriesName);
            if (forward != null)
            {
                return Redirect($"/{Uri.EscapeDataString(forward.NewClubInitials)}/" +
                    $"{forward.NewSeasonUrlName}/{forward.NewSeriesUrlName}");
            }
            else
            {
                return NotFound();
            }
        }
        var canEdit = false;
        var canEditSeries = false;
        if (User != null && (User.Identity?.IsAuthenticated ?? false))
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            canEdit = await _authService.CanUserEdit(User, clubId);
            canEditSeries = await _authService.CanUserEditSeries(User, clubId);
        }

        return View(new ClubItemViewModel<Core.Model.Series>
        {
            Item = series,
            ClubInitials = clubInitials,
            CanEdit = canEdit,
            CanEditSeries = canEditSeries
        });
    }

    public async Task<ActionResult> ExportCsv(
        string id)
    {
        Core.Model.Series series;
        try
        {
            series = await _seriesService.GetSeriesAsync(new Guid(id));
        } catch (InvalidOperationException)
        {
            series = null;
        }
        if (series == null)
        {
            return new NotFoundResult();
        }

        var filename = series.Name.Contains(series.Season.Name) ? $"{series.Name}.csv" : $"{series.Season.Name} {series.Name}.csv";
        var csv = _csvService.GetCsv(series);

        return File(csv, "text/csv", filename);
    }

    public async Task<ActionResult> ExportHtml(
        string id)
    {
        var series = await _seriesService.GetSeriesAsync(new Guid(id));
        if (series == null)
        {
            return new NotFoundResult();
        }
        var filename = series.Name.Contains(series.Season.Name) ? series.Name : $"{series.Season.Name} {series.Name}";
        // urlencode helps with unicode values, but replaces (valid) spaces.
        filename = HttpUtility.UrlEncode(filename + ".html", Encoding.UTF8);
        filename = filename.Replace("+", " ");
        var disposition = $"attachment; filename=\"{filename}\"; filename*=UTF-8''{filename}";
        Response.Headers.Append("content-disposition", disposition);

        return View(series);
    }

    public async Task<JsonResult> Chart(
        Guid seriesId)
    {
        var chartData = await _seriesService.GetChartData(seriesId);

        return Json(chartData);
    }


    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> Create(string clubInitials)
    {
        var vm = await _seriesService.GetBlankVmForCreate(clubInitials);
        var errors = _adminTipService.GetSeriesCreateErrors(vm);
        if (errors != null && errors.Count > 0)
        {
            return View("CreateErrors", errors);
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> Create(string clubInitials, SeriesWithOptionsViewModel model)
    {
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            model.ClubId = clubId;

            if (!ModelState.IsValid)
            {
                var blankVm = await _seriesService.GetBlankVmForCreate(clubInitials);
                model.SeasonOptions = blankVm.SeasonOptions;
                model.ScoringSystemOptions = blankVm.ScoringSystemOptions;
                model.SummarySeriesOptions = blankVm.SummarySeriesOptions;
                return View(model);
            }

            // Clear parent series IDs for Summary Series since they can't have parents
            if (model.Type == Core.Model.SeriesType.Summary)
            {
                model.ParentSeriesIds = null;
            }

            model.UpdatedBy = await GetUserStringAsync();
            var newSeriesId = await _seriesService.SaveNew(model);

            if (model.Type == Core.Model.SeriesType.Summary)
            {
                return RedirectToAction("Edit", new { clubInitials, id = newSeriesId });
            }

            return RedirectToAction("Index", "Admin");
        }
        catch (InvalidOperationException ex)
        {
            var blankVm = await _seriesService.GetBlankVmForCreate(clubInitials);
            model.SeasonOptions = blankVm.SeasonOptions;
            model.ScoringSystemOptions = blankVm.ScoringSystemOptions;
            model.SummarySeriesOptions = blankVm.SummarySeriesOptions;

            // Clear parent series IDs for Summary Series since they can't have parents
            // probably redundant with above, but doesn't hurt.
            if (model.Type == Core.Model.SeriesType.Summary)
            {
                model.ParentSeriesIds = null;
            }

            ModelState.AddModelError(String.Empty, ex.Message);

            return View(model);
        }
        catch
        {
            var blankVm = await _seriesService.GetBlankVmForCreate(clubInitials);
            model.SeasonOptions = blankVm.SeasonOptions;
            model.ScoringSystemOptions = blankVm.ScoringSystemOptions;
            model.SummarySeriesOptions = blankVm.SummarySeriesOptions;

            // Clear parent series IDs for Summary Series since they can't have parents
            if (model.Type == Core.Model.SeriesType.Summary)
            {
                model.ParentSeriesIds = null;
            }

            ModelState.AddModelError(String.Empty,
                "A problem occurred creating this series. Does a " +
                "series with this season and this name already exist?");

            return View(model);
        }
    }

    private async Task<string> GetUserStringAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user.GetDisplayName();
    }

    [Authorize]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> Edit(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var clubId = await _clubService.GetClubId(clubInitials);
        var series = await _seriesService.GetSeriesAsync(id);
        if (series == null)
        {
            return NotFound();
        }

        var seriesWithOptions = _mapper.Map<SeriesWithOptionsViewModel>(series);

        var blankVm = await _seriesService.GetBlankVmForCreate(clubInitials);
        seriesWithOptions.SeasonOptions = blankVm.SeasonOptions;
        seriesWithOptions.ScoringSystemOptions = blankVm.ScoringSystemOptions;
        seriesWithOptions.SummarySeriesOptions = blankVm.SummarySeriesOptions;
        if(seriesWithOptions.Type == Core.Model.SeriesType.Summary)
        {
            seriesWithOptions.SeriesOptions =
                (await _seriesService.GetChildSeriesSummariesAsync(
                    clubId,
                    seriesWithOptions.SeasonId)).ToList();
            return View("EditSummarySeries", seriesWithOptions);
        }
        return View(seriesWithOptions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> Edit(
        string clubInitials,
        SeriesWithOptionsViewModel model,
        string returnUrl = null)
    {
        try
        {
            ViewData["ReturnUrl"] = returnUrl;
            var clubId = await _clubService.GetClubId(clubInitials);
            model.ClubId = clubId;

            if (!ModelState.IsValid)
            {
                var blankVm = await _seriesService.GetBlankVmForCreate(clubInitials);
                model.SeasonOptions = blankVm.SeasonOptions;
                model.ScoringSystemOptions = blankVm.ScoringSystemOptions;
                model.SummarySeriesOptions = blankVm.SummarySeriesOptions;
                return View(model);
            }

            model.UpdatedBy = await GetUserStringAsync();
            
            // Clear parent series IDs for Summary Series since they can't have parents
            if (model.Type == Core.Model.SeriesType.Summary)
            {
                model.ParentSeriesIds = null;
            }
            
            await _seriesService.Update(model);

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            return View(model);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> Delete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var series = await _seriesService.GetSeriesAsync(id);
        if (series == null)
        {
            return NotFound();
        }
        return View(series);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        try
        {
            await _seriesService.DeleteAsync(id);

            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            ModelState.AddModelError(String.Empty,
                "A problem occurred deleting this series.");
            var series = await _seriesService.GetSeriesAsync(id);
            return View(series);
        }
    }

    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> CreateMultiple(string clubInitials)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        var vm = await _seriesService.GetBlankVmForCreateMultiple(clubInitials);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> CreateMultiple(
        string clubInitials,
        MultipleSeriesWithOptionsViewModel model)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        if (!ModelState.IsValid)
        {
            var blankVm = await _seriesService.GetBlankVmForCreateMultiple(clubInitials);
            model.SeasonOptions = blankVm.SeasonOptions;
            model.ScoringSystemOptions = blankVm.ScoringSystemOptions;
            return View(model);
        }

        try
        {
            await _seriesService.CreateMultipleAsync(clubInitials, clubId, model, await GetUserStringAsync());
            return RedirectToAction("Index", "Admin", new { clubInitials });
        }
        catch (InvalidOperationException ex)
        {
            var blankVm = await _seriesService.GetBlankVmForCreateMultiple(clubInitials);
            model.SeasonOptions = blankVm.SeasonOptions;
            model.ScoringSystemOptions = blankVm.ScoringSystemOptions;

            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch
        {
            var blankVm = await _seriesService.GetBlankVmForCreateMultiple(clubInitials);
            model.SeasonOptions = blankVm.SeasonOptions;
            model.ScoringSystemOptions = blankVm.ScoringSystemOptions;

            ModelState.AddModelError(string.Empty, "A problem occurred creating these series. Check" +
                " for duplicate names within the selected season or invalid date ranges.");
            return View(model);
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<IActionResult> ImportIcal(string clubInitials, Guid seasonId, IFormFile file, string url)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        try
        {
            var result = await _seriesService.ImportIcalAsync(clubInitials, seasonId, file, url);
            
            // Map the result to the anonymous object structure expected by the frontend
            var seriesList = result.Series.Select(s => new
            {
                Name = s.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate
            }).ToList();

            return Json(new { series = seriesList, warning = result.Warning });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest($"An error occurred: {ex.Message}");
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<IActionResult> CheckSeriesNamesUnique(
        string clubInitials,
        Guid seasonId,
        [FromBody] List<string> names)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        var existingNames = await _seriesService.GetSeriesNamesAsync(clubId, seasonId);
        var existingNamesSet = new HashSet<string>(
            existingNames.Select(n => n.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var conflictingNames = names
            .Where(n => !string.IsNullOrWhiteSpace(n) && existingNamesSet.Contains(n.Trim()))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Json(new { conflictingNames });
    }
}
