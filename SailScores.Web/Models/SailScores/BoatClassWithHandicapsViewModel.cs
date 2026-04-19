using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class BoatClassWithHandicapsViewModel
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    [StringLength(2000)]
    public string Description { get; set; }

    // null = club doesn't enable handicap scoring (hide section)
    public IList<ClassHandicap> HandicapRatings { get; set; }
}
