using System;
using Microsoft.Extensions.Logging;

namespace SailScores.Core.Services
{
    public class ConversionService : IConversionService
    {
        private readonly ILogger<ConversionService> _logger;

        public ConversionService(
            ILogger<ConversionService> logger)
        {
            _logger = logger;
        }

        public string Fahrenheit => "Fahrenheit";

        public string Celsius => "Celsius";

        public string Kelvin => "Kelvin";

        public string MeterPerSecond => "MetersPerSecond";

        public string MilesPerHour => "MPH";


        public decimal? Convert(string measure, string sourceUnits, string destinationUnits)
        {
            decimal decimalMeasure;
            if(decimal.TryParse(measure, out decimalMeasure))
            {
                return Convert(measure, sourceUnits, destinationUnits);
            }
            return null;
        }

        public decimal? Convert(decimal? measure, string sourceUnits, string destinationUnits)
        {
            if (!measure.HasValue)
            {
                return null;
            }
            return Convert(measure.Value, sourceUnits, destinationUnits);
        }
        public decimal Convert(decimal measure, string sourceUnits, string destinationUnits)
        {
            _logger.LogInformation("In Convert!");
            var sourceUnitEnum = GetUnit(sourceUnits);
            _logger.LogInformation("About to GetDestinationUnit");

            var destinationUnitEnum = GetUnit(destinationUnits);
            _logger.LogInformation("About to Compare unit types!");
            if (GetUnitType(sourceUnitEnum) != GetUnitType(destinationUnitEnum))
            {
                _logger.LogInformation("About to throw since types don't match");

                throw new InvalidOperationException(
                    $"Could not convert between {sourceUnits} and {destinationUnits}");
            }

            _logger.LogInformation("About to test temp conversion");

            if (GetUnitType(sourceUnitEnum) == UnitType.Temperature)
            {
                _logger.LogInformation("About to convert Temp");

                return ConvertTemperature(measure, sourceUnitEnum, destinationUnitEnum);
            } else if (GetUnitType(sourceUnitEnum) == UnitType.Speed)
            {
                _logger.LogInformation("About to convert speed");

                return ConvertSpeed(measure, sourceUnitEnum, destinationUnitEnum);
            }
            _logger.LogInformation("Couldn't complete conversion, so throwing");

            throw new InvalidOperationException("Could not complete conversion for the unit types.");
        }

        private decimal ConvertTemperature(
            decimal temp,
            Units sourceUnits,
            Units destinationUnits)
        {
            _logger.LogInformation("About to convertToKelvin");
            var tempInKelvin = ConvertToKelvin(temp, sourceUnits);
            _logger.LogInformation("Now convert from kelvin");
            return ConvertFromKelvin(tempInKelvin, destinationUnits);
        }

        private decimal ConvertFromKelvin(decimal temp, Units destinationUnits)
        {
            if (destinationUnits == Units.Fahrenheit)
            {
                return ((temp - 273.15m) * 9m / 5m) + 32m;
            }
            if (destinationUnits == Units.Celsius)
            {
                return temp - 273.15m;
            }
            return temp;
        }

        private decimal ConvertToKelvin(decimal temp, Units sourceUnits)
        {
            _logger.LogInformation($"Converting to Kelvin from : unit type {sourceUnits}");

            switch (sourceUnits)
            {
                case Units.Fahrenheit:
                    return (5m / 9m * (temp - 32m)) + 273.15m;
                case Units.Celsius:
                    return temp + 273.15m;
                default:
                    return temp;
            }
        }

        private decimal ConvertSpeed(
            decimal speed,
            Units sourceUnits,
            Units destinationUnits)
        {
            var speedInMeterPerSecond = ConvertToMeterPerSecond(speed, sourceUnits);
            return ConvertFromMeterPerSecond(speedInMeterPerSecond, destinationUnits);
        }

        private decimal ConvertFromMeterPerSecond(decimal speed, Units destinationUnits)
        {
            switch (destinationUnits)
            {
                case Units.MilesPerHour:
                    return speed * 2.237m;
                case Units.KilometersPerHour:
                    return speed * 3.6m;
                case Units.Knots:
                    return speed * 1.944m;
                default:
                    return speed;
            }    
        }

        private decimal ConvertToMeterPerSecond(decimal speed, Units sourceUnits)
        {
            switch (sourceUnits)
            {
                case Units.MilesPerHour:
                    return speed / 2.237m;
                case Units.KilometersPerHour:
                    return speed / 3.6m;
                case Units.Knots:
                    return speed / 1.944m;
                default:
                    return speed;
            }
        }

        private Units GetUnit(string units)
        {
            _logger.LogInformation("In GetUnit");
            _logger.LogInformation($"{units}");

            if (units.ToUpperInvariant() == "MPH")
            {
                return Units.MilesPerHour;
            }
            if (units.ToUpperInvariant() == "KM/H" || units.ToUpperInvariant() == "KPH")
            {
                return Units.KilometersPerHour;
            }
            if (units.ToUpperInvariant() == "KNOTS" || units.ToUpperInvariant().StartsWith("KNT"))
            {
                return Units.Knots;
            }
            if (units.ToUpperInvariant() == "M/S" || units.ToUpperInvariant().StartsWith("METER"))
            {
                return Units.MeterPerSecond;
            }
            _logger.LogInformation("About to check the temperature types");

            if (units.StartsWith("F", StringComparison.InvariantCultureIgnoreCase)
                || units.StartsWith("°F", StringComparison.InvariantCultureIgnoreCase))
            {
                return Units.Fahrenheit;
            }
            _logger.LogInformation("About to check for Celsius");

            if (units.StartsWith("CE", StringComparison.InvariantCultureIgnoreCase)
                || units.StartsWith("°C", StringComparison.InvariantCultureIgnoreCase))
            {
                return Units.Celsius;
            }
            _logger.LogInformation("About to check for Kelvin");
            if (units.StartsWith("KE", StringComparison.InvariantCultureIgnoreCase)
                || units.StartsWith("°K", StringComparison.InvariantCultureIgnoreCase))
            {
                return Units.Kelvin;
            }
            _logger.LogInformation("About to throw from could not convert");

            throw new InvalidOperationException("Could not convert. Unknown units");
        }

        private UnitType GetUnitType(Units unitEnum)
        {
            switch (unitEnum)
            {
                case Units.Fahrenheit:
                case Units.Celsius:
                case Units.Kelvin:
                    return UnitType.Temperature;
                case Units.KilometersPerHour:
                case Units.Knots:
                case Units.MeterPerSecond:
                case Units.MilesPerHour:
                    return UnitType.Speed;

            }
            throw new InvalidOperationException("Unit Type is not defined for conversion");
        }

        private enum Units
        {
            Fahrenheit = 1,
            Celsius = 2,
            Kelvin = 3,
            MeterPerSecond = 100,
            MilesPerHour = 101,
            KilometersPerHour = 102,
            Knots = 103
        }

        private enum UnitType
        {
            Speed = 1,
            Temperature = 2
        }
    }
}
