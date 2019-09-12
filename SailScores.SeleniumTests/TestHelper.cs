using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.SeleniumTests
{
    class TestHelper
    {

        public static IConfigurationRoot GetIConfigurationRoot(string outputPath)
        {
            return new ConfigurationBuilder()
                .SetBasePath(outputPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets("2648C85C-E8AB-4D2F-95A2-38F0C785CE62")
                .AddEnvironmentVariables()
                .Build();
        }

        public static SailScoresTestConfig GetApplicationConfiguration(string outputPath)
        {
            var configuration = new SailScoresTestConfig();

            var iConfig = GetIConfigurationRoot(outputPath);

            iConfig
                .GetSection("SailScores")
                .Bind(configuration);

            return configuration;
        }


    }
}
