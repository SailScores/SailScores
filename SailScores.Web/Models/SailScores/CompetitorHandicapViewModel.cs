using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class CompetitorHandicapViewModel
{
    public Guid Id { get; set; }

    public Guid CompetitorId { get; set; }

    [Required]
    [Display(Name = "Handicap System")]
    public Guid HandicapSystemId { get; set; }

    [Required]
    [Display(Name = "Rating")]
    public decimal Value { get; set; }

    [Display(Name = "Effective From")]
    public DateTime? EffectiveFrom { get; set; }

    [Display(Name = "Effective To")]
    public DateTime? EffectiveTo { get; set; }

    [StringLength(1000)]
    public string Notes { get; set; }

    // For display in list
    public string HandicapSystemName { get; set; }

    // Options for dropdowns
    public IList<HandicapSystem> HandicapSystemOptions { get; set; }

    // For back-navigation
    public string ClubInitials { get; set; }
    public string CompetitorName { get; set; }
}
