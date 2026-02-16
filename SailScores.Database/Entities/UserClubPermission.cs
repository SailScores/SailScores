using System.ComponentModel.DataAnnotations;

namespace SailScores.Database.Entities;

public class UserClubPermission
{
    public Guid Id { get; set; }
    [StringLength(254)]
    public string UserEmail { get; set; }
    public Guid? ClubId { get; set; }
    public bool CanEditAllClubs { get; set; }
    public PermissionLevel PermissionLevel { get; set; }
    public DateTime? Created { get; set; }
        
    [StringLength(200)]
    public string CreatedBy { get; set; }
}