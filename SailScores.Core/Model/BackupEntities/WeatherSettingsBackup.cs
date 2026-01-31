namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for weather settings.
/// </summary>
public class WeatherSettingsBackup
{
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string TemperatureUnits { get; set; }
    public string WindSpeedUnits { get; set; }
}
