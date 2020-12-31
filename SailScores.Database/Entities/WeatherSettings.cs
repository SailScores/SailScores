using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Database.Entities
{
    public class WeatherSettings
    {

        public Guid Id { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        [StringLength(20)]
        public string TemperatureUnits { get; set; }
        [StringLength(20)]
        public string WindSpeedUnits { get; set; }
    }
}