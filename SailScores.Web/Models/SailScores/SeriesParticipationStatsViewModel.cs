using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class SeriesParticipationStatsViewModel
{
    // The date range used to produce these stats (filters)
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Club navigation info
    public string ClubInitials { get; set; }
    public string ClubName { get; set; }

    // Filter: when true, show only summary series
    public bool SummaryOnly { get; set; }

    // Rows of stats returned from the SQL query
    public IEnumerable<SeriesParticipationStatsRow> Rows { get; set; } = new List<SeriesParticipationStatsRow>();

    public class SeriesParticipationStatsRow
    {
        public string SeasonName { get; set; }
        public string SeasonUrlName { get; set; }
        public DateTime? SeasonStart { get; set; }

        public string SeriesName { get; set; }
        public string SeriesType { get; set; }

        // Number of distinct classes in the series
        public int? ClassCount { get; set; }

        public string ClassName { get; set; }

        public int? RaceCount { get; set; }
        public int? CompetitorsStarted { get; set; }
        public int? DistinctCompetitorsStarted { get; set; }
        public int? DistinctDaysRaced { get; set; }
        public double? AverageCompetitorsPerRace { get; set; }
        public DateTime? FirstRace { get; set; }
        public DateTime? LastRace { get; set; }
    }
}
