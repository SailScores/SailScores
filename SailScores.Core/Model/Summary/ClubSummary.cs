using System;

namespace SailScores.Core.Model.Summary;

public class ClubSummary
{
    public Guid Id { get; set; }
    public String Name { get; set; }
    public String Initials { get; set; }
    public String Description { get; set; }
    public bool IsHidden { get; set; }
}
