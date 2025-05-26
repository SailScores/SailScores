using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model;

public class Document
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid? RegattaId { get; set; }

    [StringLength(128)]
    [Required]
    public String Name { get; set; }

    [StringLength(128)]
    public String ContentType { get; set; }
    public Byte[] FileContents { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime CreatedLocalDate { get; set; }
    [StringLength(128)]
    public String CreatedBy { get; set; }
    public Guid? PreviousVersion { get; set; }

}
