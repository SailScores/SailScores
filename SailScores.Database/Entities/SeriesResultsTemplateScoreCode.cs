using System;
using System.ComponentModel.DataAnnotations;
using SailScores.Api.Enumerations;

namespace SailScores.Database.Entities;

// How a results template shows one score code. Codes without a row are shown with code and points.
public class SeriesResultsTemplateScoreCode
{
    public Guid Id { get; set; }

    public Guid SeriesResultsTemplateId { get; set; }
    public SeriesResultsTemplate SeriesResultsTemplate { get; set; }

    [Required]
    [StringLength(20)]
    public string Code { get; set; }

    public ScoreCodeDisplay Display { get; set; }

    // Muted like a discard.
    public bool Faded { get; set; }
}
