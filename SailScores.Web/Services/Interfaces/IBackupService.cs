using System;
using System.IO;
using System.Threading.Tasks;
using SailScores.Core.Model;
using SailScores.Core.Services;

namespace SailScores.Web.Services.Interfaces;

/// <summary>
/// Web layer service for club backup operations.
/// Handles compression/decompression and file operations.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a compressed backup file for download.
    /// </summary>
    /// <param name="clubInitials">Club initials</param>
    /// <param name="createdBy">Username creating the backup</param>
    /// <returns>Compressed backup as byte array with suggested filename</returns>
    Task<(byte[] Data, string FileName)> CreateBackupFileAsync(string clubInitials, string createdBy);

    /// <summary>
    /// Reads and validates a backup file from an uploaded stream.
    /// </summary>
    /// <param name="stream">Uploaded file stream</param>
    /// <returns>Parsed backup data and validation result</returns>
    Task<(ClubBackupData Backup, BackupValidationResult Validation)> ReadBackupFileAsync(Stream stream);

    /// <summary>
    /// Restores a backup to a club.
    /// </summary>
    /// <param name="clubInitials">Target club initials</param>
    /// <param name="backup">Backup data to restore</param>
    /// <param name="preserveClubSettings">If true, club name/initials/url are not changed</param>
    /// <returns>True if successful</returns>
    Task<bool> RestoreBackupAsync(string clubInitials, ClubBackupData backup, bool preserveClubSettings = true);
}
