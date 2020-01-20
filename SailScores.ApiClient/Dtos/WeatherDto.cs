namespace SailScores.Api.Dtos
{
    public class WeatherDto
    {
        public string Description { get; set; }

        public string Icon { get; set; }

        public string TemperatureString { get; set; }
        public decimal? TemperatureDegreesKelvin { get; set; }

        public string WindSpeedString { get; set; }
        public decimal? WindSpeedMeterPerSecond { get; set; }

        public string WindDirectionString { get; set; }
        public decimal? WindDirectionDegrees { get; set; }

        public string WindGustString { get; set; }
        public decimal? WindGustMeterPerSecond { get; set; }
        public decimal? Humidity { get; set; }
        public decimal? CloudCoverPercent { get; set; }



    }
}