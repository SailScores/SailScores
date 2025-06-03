using SailScores.Api.Enumerations;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public IList<Race> Races { get; set; }

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

    // used for connecting series to correct fleet. Particularly for Regatta use.
    public Guid? FleetId { get; set; }

    public bool? PreferAlternativeSailNumbers { get; set; }

    public bool? ShowCompetitorClub { get; internal set; }

    public bool HideDncDiscards { get; set; }

    // If set, any races assigned to this series will not be
    // used in the competitor summary statistics.
    public bool ExcludeFromCompetitorStats { get; set; }

    public Series ShallowCopy()
    {
        return (Series)this.MemberwiseClone();
    }
}

public enum SeriesType
{
    Standard = 1,
    Regatta = 2,
    Summary = 3,
    // future values: Team, Match
}
#pragma warning restore CA2227 // Collection properties should be read only

