using SailScores.ImportExport.Sailwave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SailScores.ImportExport.Sailwave.Elements;
using Xunit;

namespace SailScores.ImportExport.Sailwave.Tests.Integration
{
    public class WriteFileTests
    {
        private string _simpleFilePathInput = @"..\..\..\SailwaveFiles\SimpleSeries.blw";
        private string _lhycFilePathInput = @"..\..\..\SailwaveFiles\LHYCSeries.blw";
        private string _simpleFilePathOutput = @"..\..\..\SailwaveFiles\SimpleSeriesOut.blw";
        private string _lhycFilePathOutput = @"..\..\..\SailwaveFiles\LHYCSeriesOut.blw";
        private Series _series;

        public WriteFileTests()
        {
            _series = Utilities.SimplePocosFromFile.GetSeries(_simpleFilePathInput);
        }

        [Fact]
        public async Task BasicWriteFile()
        {
            var writer = new SailwaveFileWriter();

            await writer.WriteAsync(_series, _simpleFilePathOutput);
        }


        [Fact]
        public async Task BigFileWrite()
        {
            var writer = new SailwaveFileWriter();
            var series = Utilities.SimplePocosFromFile.GetSeries(_lhycFilePathInput);
            await writer.WriteAsync(series, _lhycFilePathOutput);
        }
    }
}
