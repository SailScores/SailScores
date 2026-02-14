using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Authorization;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize(Policy = AuthorizationPolicies.ClubAdmin)]

public class BackupController : Controller
{

    private const string CLUBINITIALS_FIELDNAME = "ClubInitials";
    private const string ERROR_FIELDNAME = "Error";
    private readonly IBackupService _backupService;
    private readonly IAuthorizationService _authService;
    private readonly IClubService _clubService;

    public BackupController(
        IBackupService backupService,
        IAuthorizationService authService,
        IClubService clubService)
    {
        _backupService = backupService;
        _authService = authService;
        _clubService = clubService;
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

            // Store backup in TempData for confirmation step
            // Since ClubBackupData can be large, we'll re-read on restore
            var club = await _clubService.GetClubForClubHome(clubInitials);

            return View("Confirm", new BackupConfirmViewModel
            {
                ClubName = club.Name,
                ClubInitials = clubInitials,
                SourceClubName = validation.SourceClubName,
                BackupDate = validation.CreatedDateUtc,
                BackupVersion = validation.Version,
                FileName = backupFile.FileName
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
    public async Task<IActionResult> Restore(string clubInitials, IFormFile backupFile, bool preserveClubSettings = true)
    {
        ViewData[CLUBINITIALS_FIELDNAME] = clubInitials;

        if (backupFile == null || backupFile.Length == 0)
        {
            TempData[ERROR_FIELDNAME] = "Please select a backup file to restore.";
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

            var success = await _backupService.RestoreBackupAsync(clubInitials, backup, preserveClubSettings);

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
}
