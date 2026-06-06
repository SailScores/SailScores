using SailScores.Api.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class SeriesResultsTemplateViewModel
{
    public Guid Id { get; set; }

    public Guid ClubId { get; set; }

    public string ClubInitials { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Display(Name = "Sail Number")]
    public ColumnVisibility SailNumberVisibility { get; set; } = ColumnVisibility.Always;

    [Display(Name = "Competitor Name")]
    public ColumnVisibility CompetitorNameVisibility { get; set; } = ColumnVisibility.Always;

    [Display(Name = "Competitor Name Header")]
    [StringLength(50)]
    public string CompetitorNameHeader { get; set; } = "Helm";

    [Display(Name = "Boat Name")]
    public ColumnVisibility BoatNameVisibility { get; set; } = ColumnVisibility.OnLargerScreens;

    [Display(Name = "Boat Name Header")]
    [StringLength(50)]
    public string BoatNameHeader { get; set; } = "Boat";

    [Display(Name = "Competitor Club")]
    public ColumnVisibility CompetitorClubVisibility { get; set; } = ColumnVisibility.Hidden;

    // Used in delete view to show warning
    public bool IsClubDefault { get; set; }
}
