using Microsoft.Extensions.Logging;
using Moq;
using SailScores.Core.Model;
using SailScores.Core.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
    public class ConversionServiceTests
    {
        private readonly ConversionService _service;
        private readonly Mock<ILogger<ConversionService>> _mockLogger;


        public ConversionServiceTests()
        {
            _mockLogger = new Mock<ILogger<ConversionService>>();

            _service = new ConversionService(
                _mockLogger.Object
                );


        }

        [Fact]
        public async Task Convert_String32F_returns0C()
        {
            var result = _service.Convert("32", "Fahrenheit", "Celsius");

            Assert.Equal(0m, result);
        }


        [Fact]
        public async Task Convert_String0C_returns32F()
        {
            var result = _service.Convert("0", "Celsius", "Fahrenheit");

            Assert.Equal(32m, result);
        }


        [Fact]
        public async Task Convert_String100C_returns212F()
        {
            var result = _service.Convert("100", "Celsius", "Fahrenheit");

            Assert.Equal(212m, result);
        }


        [Fact]
        public async Task Convert_String212F_returns100C()
        {
            var result = _service.Convert("212", "Fahrenheit", "Celsius");

            Assert.True(result > 99.99m && result < 100.01m);
        }


        [Fact]
        public async Task Convert_String0Mph_returns0metersPerSecond()
        {
            var result = _service.Convert("0", "MPH", "m/s");

            Assert.Equal(0m, result);
        }

        [Fact]
        public async Task Convert_String15Mph_returns6_7metersPerSecond()
        {
            var result = _service.Convert("15", "MPH", "m/s");

            Assert.True(result > 6.7m && result < 6.71m);
        }

        [Fact]
        public async Task Convert_String15Mph_returns13Knots()
        {
            var result = _service.Convert("15", "MPH", "knots");

            Assert.True(result > 13m && result < 13.1m);
        }

        [Fact]
        public async Task Convert_String15Mph_returns24kmPerHour()
        {
            var result = _service.Convert("15", "MPH", "km/h");

            Assert.True(result > 24.1m && result < 24.2m);
        }

        [Fact]
        public async Task Convert_CannotConvertTempToSpeed()
        {
            Assert.Throws<InvalidOperationException>(
                () => _service.Convert("10", "Celsius", "MPH"));
        }
    }
}
