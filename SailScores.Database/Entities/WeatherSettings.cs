using System;

namespace SailScores.Database.Entities
{
    public class WeatherSettings
    {

        public Guid Id { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string TemperatureUnits { get; set; }
        public string WindSpeedUnits { get; set; }
    }
}