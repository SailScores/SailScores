using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using SailScores.Core.Model.BackupEntities;
using SailScores.Core.Services;
using SailScores.Web.Authorization;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;
using IBackupService = SailScores.Web.Services.Interfaces.IBackupService;
using IClubService = SailScores.Web.Services.Interfaces.IClubService;

namespace SailScores.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.ClubAdmin)]

public class BackupController : Controller
{

    private const string CLUBINITIALS_FIELDNAME = "ClubInitials";
    private const string ERROR_FIELDNAME = "Error";
    private const string BACKUP_CACHE_PREFIX = "backup_";
    private const int BACKUP_CACHE_MINUTES = 30;

    private readonly IBackupService _backupService;
    private readonly IAuthorizationService _authService;
    private readonly IClubService _clubService;
    private readonly IDistributedCache _cache;

    public BackupController(
        IBackupService backupService,
        IAuthorizationService authService,
        IClubService clubService,
        IDistributedCache cache)
    {
        _backupService = backupService;
        _authService = authService;
        _clubService = clubService;
        _cache = cache;
    }

    // GET: /{clubInitials}/Backup
    public async Task<IActionResult> Index(string clubInitials)
    {
        ViewData[CLUBINITIALS_FIELDNAME] = clubInitials;

        var club = await _clubService.GetClubForClubHome(clubInitials);
        return View(new BackupIndexViewModel
        {
            ClubName = club.Name,
            ClubInitials = clubInitials
        });
    }

    // POST: /{clubInitials}/Backup/Download
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Download(string clubInitials)
    {

        try
        {
            var (data, fileName) = await _backupService.CreateBackupFileAsync(
                clubInitials,
                User.Identity?.Name ?? "Unknown");

            return File(data, "application/gzip", fileName);
        }
        catch (Exception ex)
        {
            TempData[ERROR_FIELDNAME] = $"Error creating backup: {ex.Message}";
            return RedirectToAction(nameof(Index), new { clubInitials });
        }
    }

    // GET: /{clubInitials}/Backup/Upload
    public async Task<IActionResult> Upload(string clubInitials)
    {
        ViewData[CLUBINITIALS_FIELDNAME] = clubInitials;

        var club = await _clubService.GetClubForClubHome(clubInitials);
        return View(new BackupUploadViewModel
        {
            ClubName = club.Name,
            ClubInitials = clubInitials
        });
    }

    // POST: /{clubInitials}/Backup/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(string clubInitials, IFormFile backupFile)
    {
        ViewData[CLUBINITIALS_FIELDNAME] = clubInitials;

        if (backupFile == null || backupFile.Length == 0)
        {
            TempData[ERROR_FIELDNAME] = "Please select a backup file to upload.";
            return RedirectToAction(nameof(Upload), new { clubInitials });
        }

        try
        {
            using var stream = backupFile.OpenReadStream();
            var (backup, validation) = await _backupService.ReadBackupFileAsync(stream);

            if (!validation.IsValid)
            {
                TempData[ERROR_FIELDNAME] = $"Invalid backup file: {validation.ErrorMessage}";
                return RedirectToAction(nameof(Upload), new { clubInitials });
            }

            // Perform comprehensive dry-run validation
            var dryRunResult = await _backupService.ValidateBackupAsync(clubInitials, backup);

            // Cache the backup data temporarily (30 minutes)
            var cacheToken = Guid.NewGuid().ToString();
            var cacheKey = $"{BACKUP_CACHE_PREFIX}{cacheToken}";
            var json = JsonSerializer.Serialize(backup);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(BACKUP_CACHE_MINUTES)
            };
            await _cache.SetStringAsync(cacheKey, json, cacheOptions);

            // Store backup in TempData for confirmation step
            var club = await _clubService.GetClubForClubHome(clubInitials);

            return View("Confirm", new BackupConfirmViewModel
            {
                ClubName = club.Name,
                ClubInitials = clubInitials,
                SourceClubName = validation.SourceClubName,
                BackupDate = validation.CreatedDateUtc,
                BackupVersion = validation.Version,
                FileName = backupFile.FileName,
                DryRunResult = dryRunResult,
                BackupCacheToken = cacheToken
            });
        }
        catch (Exception ex)
        {
            TempData[ERROR_FIELDNAME] = $"Error reading backup file: {ex.Message}";
            return RedirectToAction(nameof(Upload), new { clubInitials });
        }
    }

    // POST: /{clubInitials}/Backup/Restore
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string clubInitials, string backupCacheToken, bool preserveClubName = true)
    {
        ViewData[CLUBINITIALS_FIELDNAME] = clubInitials;

        if (string.IsNullOrEmpty(backupCacheToken))
        {
            TempData[ERROR_FIELDNAME] = "Backup session expired. Please upload the file again.";
            return RedirectToAction(nameof(Upload), new { clubInitials });
        }

        try
        {
            // Retrieve backup from cache
            var cacheKey = $"{BACKUP_CACHE_PREFIX}{backupCacheToken}";
            var cachedJson = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(cachedJson))
            {
                TempData[ERROR_FIELDNAME] = "Backup session expired. Please upload the file again.";
                return RedirectToAction(nameof(Upload), new { clubInitials });
            }

            var backup = JsonSerializer.Deserialize<ClubBackupData>(cachedJson);

            if (backup == null)
            {
                TempData[ERROR_FIELDNAME] = "Failed to deserialize backup data. Please upload the file again.";
                return RedirectToAction(nameof(Upload), new { clubInitials });
            }

            // Perform comprehensive dry-run validation before restoring
            var dryRunResult = await _backupService.ValidateBackupAsync(clubInitials, backup);

            if (!dryRunResult.CanRestore)
            {
                // Show errors to user without attempting restore
                var errorList = string.Join("; ", dryRunResult.Errors);
                TempData[ERROR_FIELDNAME] = $"Cannot restore backup due to validation errors: {errorList}";
                return RedirectToAction(nameof(Upload), new { clubInitials });
            }

            var success = await _backupService.RestoreBackupAsync(clubInitials, backup, preserveClubName);

            // Clear cache after successful restore
            await _cache.RemoveAsync(cacheKey);

            if (success)
            {
                TempData["Success"] = "Backup restored successfully. All club data has been replaced.";
                return RedirectToAction(nameof(Index), new { clubInitials });
            }
            else
            {
                TempData[ERROR_FIELDNAME] = "Failed to restore backup.";
                return RedirectToAction(nameof(Upload), new { clubInitials });
            }
        }
        catch (Exception ex)
        {
            TempData[ERROR_FIELDNAME] = $"Error restoring backup: {ex.Message}";
            return RedirectToAction(nameof(Upload), new { clubInitials });
        }
    }
}

public class BackupIndexViewModel
{
    public string ClubName { get; set; }
    public string ClubInitials { get; set; }
}

public class BackupUploadViewModel
{
    public string ClubName { get; set; }
    public string ClubInitials { get; set; }
}

public class BackupConfirmViewModel
{
    public string ClubName { get; set; }
    public string ClubInitials { get; set; }
    public string SourceClubName { get; set; }
    public DateTime BackupDate { get; set; }
    public int BackupVersion { get; set; }
    public string FileName { get; set; }

    // Dry-run validation results
    public BackupDryRunResult DryRunResult { get; set; }
    public bool HasValidationIssues => DryRunResult?.ReferenceIssues?.HasIssues == true || (DryRunResult?.Warnings?.Count ?? 0) > 0;
    public bool CanProceedWithRestore => DryRunResult?.CanRestore == true;

    // Cache token for storing backup data temporarily between validation and restore
    public string BackupCacheToken { get; set; }
}
