using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SailScores.Database.Entities;

public class SystemAlert
{
    public Guid Id { get; set; }

    public string Content { get; set; }

    [Column("ExpiresUtc")]
    public DateTime ExpiresUtc { get; set; }

    [Column("CreatedDateUtc")]
    public DateTime CreatedDate { get; set; }

    [StringLength(128)]
    public string CreatedBy { get; set; }

    public bool IsDeleted { get; set; }
}
