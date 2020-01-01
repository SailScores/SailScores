namespace SailScores.Core.Model
{
    public class WeatherSettings
    {
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string TemperatureUnits { get; set; }
        public string WindSpeedUnits { get; set; }
    }
}