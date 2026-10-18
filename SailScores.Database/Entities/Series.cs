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

    public SeriesType? Type { get; set; }

    public IList<SeriesRace> RaceSeries { get; set; }

    public IList<SeriesToSeriesLink> ChildLinks { get; set; }

    public IList<SeriesToSeriesLink> ParentLinks { get; set; }

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

    public Guid? HandicapSystemId { get; set; }
    public HandicapSystem HandicapSystem { get; set; }

    public TrendOption? TrendOption { get; set; }

    // used for connecting series to correct fleet. Particularly for Regatta use.
    public Guid? FleetId { get; set; }
    public Fleet Fleet { get; set; }

    // When fleet is selected, determines whether to use full race scores (true)
    // or recalculate positions based only on fleet competitors (false)
    public bool? UseFullRaceScores { get; set; }

    public bool? PreferAlternativeSailNumbers { get; set; }

    public Guid? SeriesResultsTemplateId { get; set; }
    public SeriesResultsTemplate SeriesResultsTemplate { get; set; }

    public bool? ExcludeFromCompetitorStats { get; set; }
    public bool? HideDncDiscards { get; set; }

    // Summary series: how child series are used. Setting it also sets
    // ChildrenSeriesAsSingleRace so older code on the same database reads the same mode.
    private SummaryRollupMode _summaryRollupMode;
    public SummaryRollupMode SummaryRollupMode
    {
        get => _summaryRollupMode;
        set
        {
            _summaryRollupMode = value;
            ChildrenSeriesAsSingleRace = value == SummaryRollupMode.ChildRankAsRace;
        }
    }
    // Replaced by SummaryRollupMode. Kept so the main branch can run against the same
    // database; drop it in a separate migration once main is no longer deployed.
    public bool? ChildrenSeriesAsSingleRace { get; set; }
    // SummaryRollupMode.ChildScoreAsRace: also use the child scores of competitors the
    // child series didn't rank. Null means false.
    public bool? IncludeUnrankedChildScores { get; set; }
    public int? RaceCount { get; set; }

    public bool? DateRestricted { get; set; }
    public DateOnly? EnforcedStartDate { get; set; }
    public DateOnly? EnforcedEndDate { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}


public enum SeriesType
{
    Standard = 1,
    Regatta = 2,
    Summary = 3,
    // future values: Team, Match
}
