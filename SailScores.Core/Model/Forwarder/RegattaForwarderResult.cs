using System;

namespace SailScores.Core.Model.Forwarder;

public class RegattaForwarderResult
{
    public Guid Id { get; set; }
    public String OldClubInitials { get; set; }
    public String OldSeasonUrlName { get; set; }
    public String OldRegattaUrlName { get; set; }
    public String NewClubInitials { get; set; }
    public String NewSeasonUrlName { get; set; }
    public String NewRegattaUrlName { get; set; }
}
