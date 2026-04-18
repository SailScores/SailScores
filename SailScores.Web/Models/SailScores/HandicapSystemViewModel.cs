using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class HandicapSystemViewModel
{
    public Guid Id { get; set; }
    public Guid? ClubId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Display(Name = "System Type")]
    public HandicapSystemType SystemType { get; set; }

    [StringLength(2000)]
    public string Description { get; set; }
}
