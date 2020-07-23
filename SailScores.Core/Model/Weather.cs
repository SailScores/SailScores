using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SailScores.Core.Model
{
    public class Weather
    {
        public Guid Id { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(32)]
        public string Icon { get; set; }

        [StringLength(32)]
        public string TemperatureString { get; set; }
        public decimal? TemperatureDegreesKelvin { get; set; }

        [StringLength(32)]
        public string WindSpeedString { get; set; }
        public decimal? WindSpeedMeterPerSecond { get; set; }

        [StringLength(32)]
        public string WindDirectionString { get; set; }
        public decimal? WindDirectionDegrees { get; set; }

        [StringLength(32)]
        public string WindGustString { get; set; }
        public decimal? WindGustMeterPerSecond { get; set; }

        // hidden on ui? but from automatic weather?
        public decimal? Humidity { get; set; }
        public decimal? CloudCoverPercent { get; set; }

        [Column("CreatedDateUtc")]
        public DateTime? CreatedDate { get; set; }

    }
}
