using System;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores;

public class ParticipationViewModel : ClubBaseViewModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string GroupBy { get; set; }
    public IList<ParticipationItem> ParticipationData { get; set; }
}

public class ParticipationItem
{
    public string Period { get; set; }
    public DateTime PeriodStart { get; set; }
    public string FleetName { get; set; }
    public int DistinctSkippers { get; set; }
}
