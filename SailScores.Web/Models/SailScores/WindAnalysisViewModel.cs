using System;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores;

public class WindAnalysisViewModel : ClubBaseViewModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public IList<WindDataItem> WindData { get; set; }
    public string WindSpeedUnits { get; set; }
    public bool UseAdvancedFeatures { get; set; }
}

public class WindDataItem
{
    public DateTime Date { get; set; }
    public decimal? WindSpeed { get; set; }
    public decimal? WindDirection { get; set; }
    public int RaceCount { get; set; }
}
