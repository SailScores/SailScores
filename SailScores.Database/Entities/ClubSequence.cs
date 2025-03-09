namespace SailScores.Database.Entities;

public class ClubSequence
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public int NextValue { get; set; }
    [StringLength(20)]
    public string SequenceType { get; set; }
    public string SequencePrefix { get; set; }
    public string SequenceSuffix { get; set; }
    public Club Club { get; set; }

}