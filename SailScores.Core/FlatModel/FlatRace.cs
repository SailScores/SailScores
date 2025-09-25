using SailScores.Api.Enumerations;
using System;

namespace SailScores.Core.FlatModel
{
    public class FlatRace
    {
        public Guid Id { get; set; }

        public String Name { get; set; }
        public DateTime? Date { get; set; }
        public int Order { get; set; }
        public String Description { get; set; }

        public bool? IsSeries { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? TotalChildRaceCount { get; set; } = 0;
        public string seriesUrlName { get; set; }

        public RaceState? State { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public String UpdatedBy { get; set; }

        public Decimal? WindSpeedMeterPerSecond { get; set; }
        public Decimal? WindGustMeterPerSecond { get; set; }
        public Decimal? WindDirectionDegrees { get; set; }
        public String WeatherIcon { get; set; }


        public string WindSpeed { get; internal set; }
        public string WindGust { get; internal set; }
        public string WindSpeedUnits { get; internal set; }


    }
}