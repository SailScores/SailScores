using Microsoft.Extensions.Configuration;
using SailScores.Web.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SailScores.Test.Unit.Web.Extensions;

public class ConfigurationExtensionsTests : IDisposable
{
    private readonly string _dir;

    public ConfigurationExtensionsTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "SailScoresConfigTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "appsettings.json"),
            """{ "Level": "base", "OnlyBase": "base" }""");
        File.WriteAllText(Path.Combine(_dir, "extra.json"),
            """{ "Level": "extra", "Rate": "extra" }""");
    }

    public void Dispose()
    {
        Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void AddJsonFileAfterAppSettings_WithLaterSource_LaterSourceWins()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(_dir)
            .AddJsonFile("appsettings.json")
            .AddInMemoryCollection(new Dictionary<string, string> { ["Rate"] = "environment" });

        builder.AddJsonFileAfterAppSettings("extra.json");
        var config = builder.Build();

        Assert.Equal("extra", config["Level"]);
        Assert.Equal("environment", config["Rate"]);
        Assert.Equal("base", config["OnlyBase"]);
    }

    [Fact]
    public void AddJsonFileAfterAppSettings_NoAppSettingsSource_AddsLast()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(_dir)
            .AddInMemoryCollection(new Dictionary<string, string> { ["Rate"] = "environment" });

        builder.AddJsonFileAfterAppSettings("extra.json");
        var config = builder.Build();

        Assert.Equal("extra", config["Rate"]);
    }

    [Fact]
    public void AddJsonFileAfterAppSettings_FileMissing_DoesNotThrow()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(_dir)
            .AddJsonFile("appsettings.json");

        builder.AddJsonFileAfterAppSettings("missing.json");
        var config = builder.Build();

        Assert.Equal("base", config["Level"]);
    }
}
