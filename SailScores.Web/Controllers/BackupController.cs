using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Services.Interfaces;
using System;
using System.Threading.Tasks;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class BackupController : Controller
{
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
        ViewData["ClubInitials"] = clubInitials;
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }

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
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }

        try
        {
            var (data, fileName) = await _backupService.CreateBackupFileAsync(
                clubInitials,
                User.Identity?.Name ?? "Unknown");

            return File(data, "application/gzip", fileName);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error creating backup: {ex.Message}";
            return RedirectToAction(nameof(Index), new { clubInitials });
        }
    }

    // GET: /{clubInitials}/Backup/Upload
    public async Task<IActionResult> Upload(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }

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
        ViewData["ClubInitials"] = clubInitials;
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }

        if (backupFile == null || backupFile.Length == 0)
        {
            TempData["Error"] = "Please select a backup file to upload.";
            return RedirectToAction(nameof(Upload), new { clubInitials });
        }

        try
        {
            using var stream = backupFile.OpenReadStream();
            var (backup, validation) = await _backupService.ReadBackupFileAsync(stream);

            if (!validation.IsValid)
            {
                TempData["Error"] = $"Invalid backup file: {validation.ErrorMessage}";
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
            TempData["Error"] = $"Error reading backup file: {ex.Message}";
            return RedirectToAction(nameof(Upload), new { clubInitials });
        }
    }

    // POST: /{clubInitials}/Backup/Restore
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string clubInitials, IFormFile backupFile, bool preserveClubSettings = true)
    {
        ViewData["ClubInitials"] = clubInitials;
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }

        if (backupFile == null || backupFile.Length == 0)
        {
            TempData["Error"] = "Please select a backup file to restore.";
            return RedirectToAction(nameof(Upload), new { clubInitials });
        }

        try
        {
            using var stream = backupFile.OpenReadStream();
            var (backup, validation) = await _backupService.ReadBackupFileAsync(stream);

            if (!validation.IsValid)
            {
                TempData["Error"] = $"Invalid backup file: {validation.ErrorMessage}";
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
                TempData["Error"] = "Failed to restore backup.";
                return RedirectToAction(nameof(Upload), new { clubInitials });
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error restoring backup: {ex.Message}";
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
