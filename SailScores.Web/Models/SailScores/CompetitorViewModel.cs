using System.ComponentModel.DataAnnotations;
using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class CompetitorViewModel
{
    public Guid Id { get; set; }

    [StringLength(200)]
    public String Name { get; set; }

    [Display(Name = "Sail Number")]
    [StringLength(20)]
    public String SailNumber { get; set; }

    [Display(Name = "Boat Name")]
    [StringLength(200)]
    public String BoatName { get; set; }

    [Display(Name = "Home Club Name")]
    [StringLength(200)]
    public String HomeClubName { get; set; }

    public IList<CompetitorCustomFieldInputViewModel> CustomFieldValues { get; set; } = new List<CompetitorCustomFieldInputViewModel>();

    public override string ToString()
    {
        return BoatName + " : " + Name + " : " + SailNumber + " : " + Id;
    }
}

public class CompetitorCustomFieldInputViewModel
{
    public Guid FieldDefinitionId { get; set; }

    [StringLength(200)]
    public string Name { get; set; }

    [StringLength(100)]
    public string? DisplayHeader { get; set; }

    public CustomFieldDataType DataType { get; set; }

    [StringLength(500)]
    public string Value { get; set; }
}
