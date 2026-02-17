using System;
using System.Threading;
using System.Threading.Tasks;
using SailScores.Core.Model.BackupEntities;

using System.Collections.Generic;

namespace SailScores.Core.Services;

/// <summary>
/// Service for backing up and restoring club data.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a complete backup of all club data.
    /// Excludes scorekeeper information except for last modified user names.
    /// Uses ReadUncommitted isolation level for minimal database impact.
    /// </summary>
    /// <param name="clubId">The club ID to backup</param>
    /// <param name="createdBy">Username of the person creating the backup</param>
    /// <param name="cancellationToken">Cancellation token for aborting long-running backup</param>
    /// <returns>Complete club backup data</returns>
    Task<ClubBackupData> CreateBackupAsync(Guid clubId, string createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores club data from a backup.
    /// All existing data in the target club will be replaced.
    /// GUIDs are remapped to allow moving backups between clubs.
    /// The entire restore operation is wrapped in a transaction for atomicity.
    /// Club initials are always preserved from the target club. Club URL is always restored from the backup.
    /// </summary>
    /// <param name="targetClubId">The club to restore into</param>
    /// <param name="backup">The backup data to restore</param>
    /// <param name="preserveClubName">If true, club name is not changed (default true). Initials are always preserved, URL is always restored.</param>
    /// <param name="cancellationToken">Cancellation token for aborting restore</param>
    /// <returns>True if successful</returns>
    Task<bool> RestoreBackupAsync(Guid targetClubId, ClubBackupData backup, bool preserveClubName = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a backup file for compatibility before restoration (quick validation).
    /// </summary>
    /// <param name="backup">The backup to validate</param>
    /// <returns>Validation result with any errors</returns>
    BackupValidationResult ValidateBackup(ClubBackupData backup);

    /// <summary>
    /// Performs a comprehensive dry-run validation of a backup against a target club.
    /// Checks GUID remapping feasibility, reference consistency, and potential conflicts
    /// without modifying the database.
    /// </summary>
    /// <param name="targetClubId">The club to validate restoration into</param>
    /// <param name="backup">The backup data to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed dry-run result with warnings and errors</returns>
    Task<BackupDryRunResult> ValidateBackupAsync(Guid targetClubId, ClubBackupData backup, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of quick backup validation.
/// </summary>
public class BackupValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
    public int Version { get; set; }
    public string SourceClubName { get; set; }
    public DateTime CreatedDateUtc { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Result of comprehensive backup dry-run validation.
/// </summary>
public class BackupDryRunResult
{
    public bool IsValid { get; set; }
    public bool CanRestore { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Summary of entities that would be restored.
    /// </summary>
    public BackupEntitySummary EntitySummary { get; set; }

    /// <summary>
    /// Summary of potential reference issues found.
    /// </summary>
    public BackupReferenceIssues ReferenceIssues { get; set; }

    public string SourceClubName { get; set; }
    public DateTime CreatedDateUtc { get; set; }
    public int Version { get; set; }
}

/// <summary>
/// Summary of entities in a backup.
/// </summary>
public class BackupEntitySummary
{
    public int BoatClassCount { get; set; }
    public int SeasonCount { get; set; }
    public int FleetCount { get; set; }
    public int CompetitorCount { get; set; }
    public int ScoringSystemCount { get; set; }
    public int SeriesCount { get; set; }
    public int RaceCount { get; set; }
    public int ScoreCount { get; set; }
    public int RegattaCount { get; set; }
    public int AnnouncementCount { get; set; }
    public int DocumentCount { get; set; }
}

/// <summary>
/// Details about reference issues found during dry-run validation.
/// </summary>
public class BackupReferenceIssues
{
    public List<string> OrphanedCompetitorBoatClasses { get; set; } = new();
    public List<string> OrphanedFleetBoatClasses { get; set; } = new();
    public List<string> OrphanedScoreCompetitors { get; set; } = new();
    public List<string> OrphanedSeriesSeasons { get; set; } = new();
    public List<string> OrphanedSeriesFleets { get; set; } = new();
    public List<string> OrphanedSeriesScoringSystems { get; set; } = new();
    public List<string> UnresolvableParentScoringSystems { get; set; } = new();
    public bool HasIssues => OrphanedCompetitorBoatClasses.Count > 0
        || OrphanedFleetBoatClasses.Count > 0
        || OrphanedScoreCompetitors.Count > 0
        || OrphanedSeriesSeasons.Count > 0
        || OrphanedSeriesFleets.Count > 0
        || OrphanedSeriesScoringSystems.Count > 0
        || UnresolvableParentScoringSystems.Count > 0;
}
