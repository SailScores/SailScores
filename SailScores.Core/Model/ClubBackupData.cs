using System;
using System.Collections.Generic;

namespace SailScores.Core.Model;

/// <summary>
/// Complete backup of a club's data for export/import.
/// Excludes scorekeeper information except for last modified user names.
/// </summary>
public class ClubBackupData
{
    public ClubBackupMetadata Metadata { get; set; }

    // Club settings
    public string Name { get; set; }
    public string Initials { get; set; }
    public string Description { get; set; }
    public string HomePageDescription { get; set; }
    public bool IsHidden { get; set; }
    public bool? ShowClubInResults { get; set; }
    public bool? ShowCalendarInNav { get; set; }
    public string Url { get; set; }
    public string Locale { get; set; }
    public int? DefaultRaceDateOffset { get; set; }
    public string StatisticsDescription { get; set; }
    public WeatherSettings WeatherSettings { get; set; }

    // Referenced data
    public IList<BoatClass> BoatClasses { get; set; }
    public IList<Season> Seasons { get; set; }
    public IList<Fleet> Fleets { get; set; }
    public IList<Competitor> Competitors { get; set; }
    public IList<ScoringSystem> ScoringSystems { get; set; }
    public IList<Series> Series { get; set; }
    public IList<Race> Races { get; set; }
    public IList<Regatta> Regattas { get; set; }
    public IList<Announcement> Announcements { get; set; }
    public IList<Document> Documents { get; set; }

    // Default scoring system name for reference
    public string DefaultScoringSystemName { get; set; }
}

/// <summary>
/// Metadata about the backup file.
/// </summary>
public class ClubBackupMetadata
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public DateTime CreatedDateUtc { get; set; }
    public string SourceClubInitials { get; set; }
    public string SourceClubName { get; set; }
    public Guid SourceClubId { get; set; }
    public string CreatedBy { get; set; }
}
