using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class SeasonWithOptionsViewModel
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime Start { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime End { get; set; }

    public Guid? DefaultScoringSystemId { get; set; }

    public IList<ScoringSystem> ScoringSystemOptions { get; set; }
}
