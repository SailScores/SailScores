using System.ComponentModel.DataAnnotations;
using SailScores.Database.Entities;

namespace SailScores.Web.Models.SailScores;

public class UserViewModel
{
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name="Email Address")]
    public string EmailAddress { get; set; }
    public string Name { get; set; }
    public bool Registered { get; set; }
    public string CreatedBy { get; set; }
    public DateTime Created { get; set; }
    
    [Display(Name = "Permission Level")]
    public PermissionLevel PermissionLevel { get; set; }
}