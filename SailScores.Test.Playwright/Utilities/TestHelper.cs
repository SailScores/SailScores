using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.Test.Playwright.Utilities;

class TestHelper
{

    public static IConfigurationRoot GetIConfigurationRoot(string outputPath)
    {
        return new ConfigurationBuilder()
            .SetBasePath(outputPath)
            .AddJsonFile("appsettings.json", optional: true)
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
