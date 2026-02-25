using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Metadata about the backup file.
/// </summary>
public class ClubBackupMetadata
{
    public const int CurrentVersion = 1;
    public const string SchemaIdentifier = "sailscores-club-backup";

    /// <summary>
    /// Identifies this file as a SailScores club backup.
    /// </summary>
    public string Schema { get; set; } = SchemaIdentifier;

    public int Version { get; set; } = CurrentVersion;
    public DateTime CreatedDateUtc { get; set; }
    public string SourceClubInitials { get; set; }
    public string SourceClubName { get; set; }
    public Guid SourceClubId { get; set; }
    public string CreatedBy { get; set; }

    // Entity counts for quick validation
    public int? BoatClassCount { get; set; }
    public int? CompetitorCount { get; set; }
    public int? FleetCount { get; set; }
    public int? RaceCount { get; set; }
    public int? ScoreCount { get; set; }
    public int? SeasonCount { get; set; }
    public int? SeriesCount { get; set; }
    public int? RegattaCount { get; set; }
    public int? ScoringSystemCount { get; set; }
}
