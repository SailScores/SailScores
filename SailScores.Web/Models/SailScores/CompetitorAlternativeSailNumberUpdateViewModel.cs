using System.ComponentModel.DataAnnotations;
using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class CompetitorAlternativeSailNumberUpdateViewModel
{
    [Required]
    public Guid CompetitorId { get; set; }

    [StringLength(20)]
    public string AlternativeSailNumber { get; set; }

    [Required]
    public AltSailNumberConflictResolution ConflictResolution { get; set; } = AltSailNumberConflictResolution.AllowDuplicates;
}
