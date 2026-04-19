using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class ClassHandicapViewModel
{
    public Guid Id { get; set; }

    public Guid BoatClassId { get; set; }

    [Required]
    [Display(Name = "Handicap System")]
    public Guid HandicapSystemId { get; set; }

    [Required]
    [Display(Name = "Rating")]
    public decimal Value { get; set; }

    [Display(Name = "Effective From")]
    public DateTime? EffectiveFrom { get; set; }

    [StringLength(1000)]
    public string Notes { get; set; }

    // For display / navigation
    public string HandicapSystemName { get; set; }
    public IList<HandicapSystem> HandicapSystemOptions { get; set; }
    public string ClubInitials { get; set; }
    public string ClassName { get; set; }
}
