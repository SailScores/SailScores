using System;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services;

/// <summary>
/// Service for backing up and restoring club data.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a complete backup of all club data.
    /// Excludes scorekeeper information except for last modified user names.
    /// </summary>
    /// <param name="clubId">The club ID to backup</param>
    /// <param name="createdBy">Username of the person creating the backup</param>
    /// <returns>Complete club backup data</returns>
    Task<ClubBackupData> CreateBackupAsync(Guid clubId, string createdBy);

    /// <summary>
    /// Restores club data from a backup.
    /// All existing data in the target club will be replaced.
    /// GUIDs are remapped to allow moving backups between clubs.
    /// </summary>
    /// <param name="targetClubId">The club to restore into</param>
    /// <param name="backup">The backup data to restore</param>
    /// <param name="preserveClubSettings">If true, club name/initials/url are not changed</param>
    /// <returns>True if successful</returns>
    Task<bool> RestoreBackupAsync(Guid targetClubId, ClubBackupData backup, bool preserveClubSettings = true);

    /// <summary>
    /// Validates a backup file for compatibility before restoration.
    /// </summary>
    /// <param name="backup">The backup to validate</param>
    /// <returns>Validation result with any errors</returns>
    BackupValidationResult ValidateBackup(ClubBackupData backup);
}

/// <summary>
/// Result of backup validation.
/// </summary>
public class BackupValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
    public int Version { get; set; }
    public string SourceClubName { get; set; }
    public DateTime CreatedDateUtc { get; set; }
}
