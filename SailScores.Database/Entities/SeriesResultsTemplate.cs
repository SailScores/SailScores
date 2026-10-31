using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SailScores.Api.Enumerations;

namespace SailScores.Database.Entities;

public class SeriesResultsTemplate
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Club Club { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    // Column visibility settings
    public ColumnVisibility SailNumberVisibility { get; set; }

    // Header text; empty uses the default, translated header.
    [StringLength(100)]
    public string SailNumberHeader { get; set; }

    public ColumnVisibility AlternativeSailNumberVisibility { get; set; } = ColumnVisibility.Hidden;

    [StringLength(100)]
    public string AlternativeSailNumberHeader { get; set; }

    public ColumnVisibility CompetitorNameVisibility { get; set; }

    [StringLength(100)]
    public string CompetitorNameHeader { get; set; }

    public ColumnVisibility BoatNameVisibility { get; set; }

    [StringLength(100)]
    public string BoatNameHeader { get; set; }

    public ColumnVisibility CompetitorClubVisibility { get; set; }

    [StringLength(100)]
    public string CompetitorClubHeader { get; set; }

    // Empty uses the scoring system's total header.
    [StringLength(100)]
    public string TotalHeader { get; set; }

    // Kept in step with PreDiscardTotalVisibility so the previous release still runs on the
    // migrated database. Remove in a later release.
    public bool ShowPreDiscardTotal { get; set; } = false;
    public bool ShowClubLogo { get; set; }

    // Scoring columns every scoring system has. Calculator-specific columns are set on the
    // scoring system.
    public ColumnVisibility PreDiscardTotalVisibility { get; set; } = ColumnVisibility.Hidden;
    public ColumnVisibility ParticipationVisibility { get; set; } = ColumnVisibility.Hidden;

    // Score display settings
    public bool HighlightTopThree { get; set; } = true;
    public ResultsColumnSeparator ColumnSeparators { get; set; }
    public ColumnVisibility RaceColumnsVisibility { get; set; } = ColumnVisibility.Always;
    public bool TotalsSeparator { get; set; }
    // Wind and weather in the race column headers, for races that have it.
    public bool ShowWeather { get; set; } = true;

    // Discarded score display
    public bool DiscardStrikethrough { get; set; } = true;
    public bool DiscardFaded { get; set; } = true;

    public IList<SeriesResultsTemplateCustomField> CustomFields { get; set; } = new List<SeriesResultsTemplateCustomField>();

    public IList<SeriesResultsTemplateScoreCode> ScoreCodes { get; set; } = new List<SeriesResultsTemplateScoreCode>();
}
