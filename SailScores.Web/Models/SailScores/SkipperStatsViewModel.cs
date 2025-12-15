using System;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores;

public class SkipperStatsViewModel : ClubBaseViewModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public IList<SkipperStatItem> SkipperStats { get; set; }
}

public class SkipperStatItem
{
    public Guid CompetitorId { get; set; }
    public string CompetitorName { get; set; }
    public string SailNumber { get; set; }
    public string FleetName { get; set; }
    public string SeasonName { get; set; }
    public int RacesParticipated { get; set; }
    public int TotalFleetRaces { get; set; }
    public int BoatsBeat { get; set; }
    public decimal ParticipationPercentage { get; set; }
}
