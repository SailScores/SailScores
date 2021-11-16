using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class WeatherViewModel
{
    [StringLength(1000)]
    public string Description { get; set; }

    [StringLength(32)]
    public string Icon { get; set; }

    [StringLength(32)]
    public string Temperature { get; set; }

    [StringLength(32)]
    public string TemperatureUnits { get; set; }

    [StringLength(32)]
    public string WindSpeed { get; set; }

    [StringLength(32)]
    public string WindSpeedUnits { get; set; }

    [StringLength(32)]
    public string WindDirection { get; set; }

    [StringLength(32)]
    public string WindGust { get; set; }

    // hidden on ui? but from automatic weather?
    public decimal? Humidity { get; set; }
    public decimal? CloudCoverPercent { get; set; }

    public string TemperatureLabel
    {
        get
        {
            if (String.IsNullOrWhiteSpace(TemperatureUnits))
            {
                return String.Empty;
            }
            else if (TemperatureUnits.StartsWith("F", StringComparison.InvariantCultureIgnoreCase))
            {
                return "°F";
            }
            return "°C";
        }
    }

}