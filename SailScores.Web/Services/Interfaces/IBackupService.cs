using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SailScores.Core.Model.BackupEntities;
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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compressed backup as byte array with suggested filename</returns>
    Task<(byte[] Data, string FileName)> CreateBackupFileAsync(string clubInitials, string createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads and validates a backup file from an uploaded stream.
    /// </summary>
    /// <param name="stream">Uploaded file stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed backup data and validation result</returns>
    Task<(ClubBackupData Backup, BackupValidationResult Validation)> ReadBackupFileAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs comprehensive dry-run validation of a backup against a target club.
    /// Checks for reference integrity and potential conflicts without modifying the database.
    /// </summary>
    /// <param name="clubInitials">Target club initials</param>
    /// <param name="backup">Backup data to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed dry-run validation result</returns>
    Task<BackupDryRunResult> ValidateBackupAsync(string clubInitials, ClubBackupData backup, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a backup to a club.
    /// Initials are always preserved. URL is always restored from the backup.
    /// </summary>
    /// <param name="clubInitials">Target club initials</param>
    /// <param name="backup">Backup data to restore</param>
    /// <param name="preserveClubName">If true, the club name is not changed (default true). Initials are always preserved, URL is always restored.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> RestoreBackupAsync(string clubInitials, ClubBackupData backup, bool preserveClubName = true, CancellationToken cancellationToken = default);
}
