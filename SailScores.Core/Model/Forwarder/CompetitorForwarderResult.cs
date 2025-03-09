using System;

namespace SailScores.Core.Model.Forwarder;

public class CompetitorForwarderResult
{
    public Guid Id { get; set; }
    public String OldClubInitials { get; set; }
    public String OldUrlName { get; set; }
    public String NewClubInitials { get; set; }
    public String NewUrlName { get; set; }
}
