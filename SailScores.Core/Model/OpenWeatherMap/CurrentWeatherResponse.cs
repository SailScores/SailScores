using System.Collections.Generic;

namespace SailScores.Core.Model.OpenWeatherMap
{
    public class CurrentWeatherResponse
    {
        public Coordinates Coord { get; set; }
        public IEnumerable<OpenMapWeather> Weather { get; set; }
        public OpenMapMain Main { get; set; }
        public OpenMapWind Wind { get; set; }
        public OpenMapClouds Clouds { get; set; }

    }
}
