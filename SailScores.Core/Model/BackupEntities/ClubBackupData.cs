using System;
using System.Collections.Generic;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Complete backup of a club's data for export/import.
/// Uses dedicated backup entities instead of domain models.
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
    public WeatherSettingsBackup WeatherSettings { get; set; }

    // Referenced data
    public IList<BoatClassBackup> BoatClasses { get; set; }
    public IList<SeasonBackup> Seasons { get; set; }
    public IList<FleetBackup> Fleets { get; set; }
    public IList<CompetitorBackup> Competitors { get; set; }
    public IList<ScoringSystemBackup> ScoringSystems { get; set; }
    public IList<SeriesBackup> Series { get; set; }
    public IList<RaceBackup> Races { get; set; }
    public IList<RegattaBackup> Regattas { get; set; }
    public IList<AnnouncementBackup> Announcements { get; set; }
    public IList<DocumentBackup> Documents { get; set; }

    // Default scoring system name for reference
    public string DefaultScoringSystemName { get; set; }
}
