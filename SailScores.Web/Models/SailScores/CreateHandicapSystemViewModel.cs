using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class CreateHandicapSystemViewModel
{
    public Guid ClubId { get; set; }

    [Display(Name = "Base Handicap System")]
    [Required]
    public Guid ParentSystemId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(2000)]
    public string Description { get; set; }

    public IList<HandicapSystemSummary> BaseSystemOptions { get; set; }
}
