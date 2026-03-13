using SailScores.Api.Enumerations;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace SailScores.Core.Model;


#pragma warning disable CA2227 // Collection properties should be read only
public class Series
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }

    [Required]
    [StringLength(200)]
    public String Name { get; set; }

#pragma warning disable CA1056 // Uri properties should not be strings
    [StringLength(200)]
    public String UrlName { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

    [StringLength(2000)]
    public String Description { get; set; }

    public SeriesType Type { get; set; }
    public bool ChildrenSeriesAsSingleRace { get; set; }

    public IList<Guid> ChildrenSeriesIds { get; set; } = new List<Guid>();

    public IList<Guid> ParentSeriesIds { get; set; } = new List<Guid>();

    public IList<Race> Races { get; set; }

    public int? RaceCount { get; set; }

    public Season Season { get; set; }

    [NotMapped]
    public SeriesResults Results { get; set; }

    public IList<Competitor> Competitors { get; set; }

    public FlatModel.FlatResults FlatResults { get; set; }

    public bool IsImportantSeries { get; set; }

    public bool ResultsLocked { get; set; }


    public DateTime? UpdatedDate { get; set; }
    public String UpdatedBy { get; set; }

    public Guid? ScoringSystemId { get; set; }
    public ScoringSystem ScoringSystem { get; set; }

    public TrendOption? TrendOption { get; set; }

    public Guid? FleetId { get; set; }
    public Fleet Fleet { get; set; }

    // When fleet is selected, determines whether to use full race scores (true)
    // or recalculate positions based only on fleet competitors (false)
    public bool? UseFullRaceScores { get; set; }

    public bool? PreferAlternativeSailNumbers { get; set; }

    public bool? ShowCompetitorClub { get; internal set; }

    public bool HideDncDiscards { get; set; }

    // If set, any races assigned to this series will not be
    // used in the competitor summary statistics.
    public bool ExcludeFromCompetitorStats { get; set; }

    public bool? DateRestricted { get; set; }
    public DateOnly? EnforcedStartDate { get; set; }
    public DateOnly? EnforcedEndDate { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Gets the effective fleet for this series.
    /// For admin views, PopulateSeriesFleets() should be called first to populate
    /// the Fleet navigation property from the most recent race's fleet.
    /// </summary>
    public Fleet GetEffectiveFleet()
    {
        // Return the direct fleet assignment (which may have been populated by PopulateSeriesFleets)
        return Fleet;
    }

    /// <summary>
    /// Gets the display name of the effective fleet for this series.
    /// Summary series return "Summary", others return their fleet name or "No Fleet".
    /// </summary>
    public String GetEffectiveFleetName()
    {
        // Summary series are grouped separately
        if (Type == SeriesType.Summary)
        {
            return "Summary";
        }

        var fleet = GetEffectiveFleet();
        return fleet?.Name ?? "No Fleet";
    }

    public Series ShallowCopy()
    {
        return (Series)this.MemberwiseClone();
    }
}

public enum SeriesType
{
    Unknown = 0,
    Standard = 1,
    Regatta = 2,
    Summary = 3,
    // future values: Team, Match
}
#pragma warning restore CA2227 // Collection properties should be read only

