using System;

namespace SailScores.Core.Model.Forwarder;

public class CompetitorForwarderResult
{
    public Guid Id { get; set; }
    public String OldClubInitials { get; set; }
    public String OldSailNumber { get; set; }
    public String NewClubInitials { get; set; }
    public String NewSailNumber { get; set; }
}
