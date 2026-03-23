using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class CompetitorAlternativeSailNumberUpdateViewModel
{
    [Required]
    public Guid CompetitorId { get; set; }

    [StringLength(20)]
    public string AlternativeSailNumber { get; set; }
}
