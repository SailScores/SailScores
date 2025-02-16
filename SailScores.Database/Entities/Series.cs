using SailScores.Api.Enumerations;

namespace SailScores.Database.Entities;

public class Series
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }

    [Required]
    [StringLength(200)]
    public String Name { get; set; }

    [StringLength(200)]
    public String UrlName { get; set; }

    [StringLength(2000)]
    public String Description { get; set; }

    public IList<SeriesRace> RaceSeries { get; set; }

    [Required]
    public Season Season { get; set; }

    public bool? IsImportantSeries { get; set; }

    public bool? ResultsLocked { get; set; }

    [Column("UpdatedDateUtc")]
    public DateTime? UpdatedDate { get; set; }
    [StringLength(128)]
    public String UpdatedBy { get; set; }

    public Guid? ScoringSystemId { get; set; }
    public ScoringSystem ScoringSystem { get; set; }

    public TrendOption? TrendOption { get; set; }

    // used for connecting series to correct fleet. Particularly for Regatta use.
    public Guid? FleetId { get; set; }

    public bool? PreferAlternativeSailNumbers { get; set; }

    public bool? ExcludeFromCompetitorStats { get; set; }
    public bool? HideDncDiscards { get; set; }
}