using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class CompetitorIndexViewModel
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

    [Display(Name = "Active?")]
    public bool IsActive { get; set; }

    [StringLength(2000)]
    public String Notes { get; set; }

    public BoatClass BoatClass { get; set; }

    public bool IsDeletable { get; internal set; }
    public object PreventDeleteReason { get; internal set; }

    public override string ToString()
    {
        return BoatName + " : " + Name + " : " + SailNumber + " : " + Id;
    }

}