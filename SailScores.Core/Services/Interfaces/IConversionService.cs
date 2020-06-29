namespace SailScores.Core.Services
{
    public interface IConversionService
    {
        string Fahrenheit { get; }
        string Celsius { get; }
        string Kelvin { get; }
        string MeterPerSecond { get; }
        string MilesPerHour { get; }

        decimal Convert(decimal measure, string sourceUnits, string destinationUnits);
        decimal? Convert(decimal? measure, string sourceUnits, string destinationUnits);
        decimal? Convert(string measure, string sourceUnits, string destinationUnits);
    }
}