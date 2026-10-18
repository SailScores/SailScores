using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SailScores.Core.Model.BackupEntities;

namespace SailScores.Test.Unit.Core.Scoring.Characterization;

// A series cut from a real club backup by SailScores.FixtureTool. Fixtures live in
// Core/Scoring/Fixtures as .json or .json.gz; see the tool's README for how to make more.
public sealed class ScoringFixture
{
    private const string SharedSystemsFile = "shared-systems.json";

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Directory => Path.Combine(AppContext.BaseDirectory, "Core", "Scoring", "Fixtures");

    public ClubBackupData Backup { get; private init; }
    public string SeriesName { get; private init; }
    public string SeasonUrlName { get; private init; }
    public string SeriesUrlName { get; private init; }

    public static IReadOnlyList<string> Names() =>
        System.IO.Directory.GetFiles(Directory)
            .Select(Path.GetFileName)
            .Where(f => f != SharedSystemsFile && (f.EndsWith(".json") || f.EndsWith(".json.gz")))
            .Select(NameOf)
            .Order()
            .ToList();

    public static ScoringFixture Load(string name)
    {
        var path = Path.Combine(Directory, name + ".json");
        return LoadFile(File.Exists(path) ? path : path + ".gz");
    }

    // A fixture file anywhere, such as in a local corpus (see CorpusParityTests).
    public static ScoringFixture LoadFile(string path)
    {
        var json = path.EndsWith(".gz") ? ReadGzip(path) : File.ReadAllText(path);
        var info = JsonNode.Parse(json)!["fixtureInfo"]!;
        return new ScoringFixture
        {
            Backup = JsonSerializer.Deserialize<ClubBackupData>(json, ReadOptions),
            SeriesName = info["seriesName"]!.GetValue<string>(),
            SeasonUrlName = info["seasonUrlName"]!.GetValue<string>(),
            SeriesUrlName = info["seriesUrlName"]!.GetValue<string>()
        };
    }

    // The shared base systems, exported from the database by the fixture tool.
    public static IReadOnlyList<ScoringSystemBackup> SharedSystems()
    {
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(Directory, SharedSystemsFile)))!;
        return json["scoringSystems"].Deserialize<List<ScoringSystemBackup>>(ReadOptions);
    }

    // Settings a club's own system adds below a shared base system, so a variant run uses
    // every rule of that calculator. Keyed by shared system name.
    private static readonly Dictionary<string, JsonObject> VariantSettings = new()
    {
        // Each fleet's child system sets its penalty points.
        ["Wawasee 2026"] = new JsonObject { ["PenaltyScoreValue"] = 8 }
    };

    // Scores every series in the fixture with a shared base system instead of the club's,
    // to run one calculator over real race data.
    public void UseSharedSystem(string systemName)
    {
        var system = SharedSystems().Single(s => s.Name == systemName);
        if (VariantSettings.TryGetValue(systemName, out var extra))
        {
            var settings = JsonNode.Parse(system.Settings ?? "{}")!.AsObject();
            foreach (var (key, value) in extra)
            {
                settings[key] = value?.DeepClone();
            }
            system.Settings = settings.ToJsonString();
        }
        if (Backup.ScoringSystems.All(s => s.Id != system.Id))
        {
            Backup.ScoringSystems.Add(system);
        }
        foreach (var series in Backup.Series)
        {
            series.ScoringSystemId = system.Id;
        }
        foreach (var regatta in Backup.Regattas ?? [])
        {
            regatta.ScoringSystemId = system.Id;
        }
        foreach (var season in Backup.Seasons ?? [])
        {
            season.DefaultScoringSystemId = system.Id;
        }
        Backup.DefaultScoringSystemName = system.Name;
    }

    public static string NameOf(string fileName) =>
        fileName.EndsWith(".gz") ? fileName[..^".json.gz".Length] : fileName[..^".json".Length];

    private static string ReadGzip(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        return reader.ReadToEnd();
    }
}
