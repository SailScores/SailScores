using System;

namespace SailScores.Core.Model;

public class CompetitorChange
{
    public String ChangedBy { get; set; }
    public DateTime ChangeTimeStamp { get; set; }

    public string NewValue { get; set; }

    public string Summary { get; set; }

}