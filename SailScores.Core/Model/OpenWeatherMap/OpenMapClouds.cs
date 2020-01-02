using Newtonsoft.Json;

namespace SailScores.Core.Model.OpenWeatherMap
{
    public class OpenMapClouds
    {

        [JsonProperty("all")]
        public int CoverPercent { get; set; }
    }
}