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

    public ColumnVisibility CompetitorNameVisibility { get; set; }

    [StringLength(100)]
    public string CompetitorNameHeader { get; set; }

    public ColumnVisibility BoatNameVisibility { get; set; }

    [StringLength(100)]
    public string BoatNameHeader { get; set; }

    public ColumnVisibility CompetitorClubVisibility { get; set; }
}
