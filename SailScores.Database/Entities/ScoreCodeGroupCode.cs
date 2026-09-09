using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SailScores.Database.Entities;

public class ScoreCodeGroupCode
{
    public Guid ScoreCodeGroupId { get; set; }

    [StringLength(20)]
    public string CodeName { get; set; }

    [ForeignKey("ScoreCodeGroupId")]
    public ScoreCodeGroup ScoreCodeGroup { get; set; }
}
