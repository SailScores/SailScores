using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class ResetClubViewModel
{
    public Guid ClubId { get; set; }
    
    public string ClubName { get; set; }
    
    public string ClubInitials { get; set; }
    
    [Required(ErrorMessage = "Please select a reset level")]
    [Display(Name = "Reset Level")]
    public ResetLevel? ResetLevel { get; set; }
    
    [MustBeTrue(ErrorMessage = "You must confirm you understand this action cannot be undone")]
    [Display(Name = "I understand this action cannot be undone")]
    public bool ConfirmReset { get; set; }
}

/// <summary>
/// Validation attribute that requires a boolean property to be true.
/// </summary>
public class MustBeTrueAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        return value is bool boolValue && boolValue;
    }
}
