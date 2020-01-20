using Newtonsoft.Json;

namespace SailScores.Core.Model.OpenWeatherMap
{
    public class OpenMapWind
    {

        public decimal Speed { get; set; }

        [JsonProperty("deg")]
        public int Degrees { get; set; }

        [JsonProperty("gust")]
        public decimal? Gust { get; set; }

    }
}