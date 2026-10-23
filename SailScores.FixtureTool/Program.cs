using System.Text.RegularExpressions;
using SailScores.FixtureTool;

const string Usage = """
    SailScores fixture tool: turns club backups into scoring test fixtures.

    Commands:
      shared-systems --connection <connection string> --out <file.json> [--only <system id> ...]
          Exports the shared (site-wide) scoring and handicap systems. Club backups
          leave these out, but extraction needs them to make fixtures that score.
          With --only, updates just those scoring systems in an existing <file.json>
          and leaves the rest of it alone.

      list <backup.json.gz> [--shared <file.json>] [--out <file.csv>]
          Writes one CSV row per series: calculator, discard method, codes used,
          ties and so on, for choosing a set of fixtures that covers every feature.

      extract <backup.json.gz> --out <directory> [--shared <file.json>]
              (--series <id | urlName | season/urlName> ... | --all) [--gzip]
          Writes one fixture per series. A fixture is a club backup cut down to that
          series and what it needs to be scored, with user names removed and
          competitor names replaced by initials (CompSJ).

      anonymize <fixture.json[.gz]> ...
          Replaces competitor names by initials in fixtures written before extract
          did it. Rewrites each file in place; running it again changes nothing.
    """;

try
{
    return args.FirstOrDefault() switch
    {
        "shared-systems" => SharedSystems(new Options(args[1..])),
        "list" => List(new Options(args[1..])),
        "extract" => Extract(new Options(args[1..])),
        "anonymize" => Anonymize(args[1..]),
        _ => ShowUsage()
    };
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    return ShowUsage();
}

static int ShowUsage()
{
    Console.Error.WriteLine(Usage);
    return 1;
}

static int SharedSystems(Options options)
{
    var outPath = options.Required("--out");
    var only = options.All("--only")
        .Select(id => Guid.TryParse(id, out var g) ? g : throw new ArgumentException($"--only needs a system id, not '{id}'."))
        .ToList();
    var exported = SharedSystemsExporter.Export(options.Required("--connection"));
    var shared = exported;
    if (only.Count > 0)
    {
        shared = File.Exists(outPath)
            ? Json.ReadFile(outPath)
            : throw new ArgumentException($"--only updates an earlier export, and {outPath} doesn't exist.");
        SharedSystemsExporter.Merge(shared, exported, only);
    }
    Json.WriteFile(outPath, shared, gzip: false);
    Console.WriteLine($"Wrote {Json.Count(shared, "scoringSystems")} scoring systems and "
        + $"{Json.Count(shared, "handicapSystems")} handicap systems to {outPath}.");
    return 0;
}

static int List(Options options)
{
    var index = LoadIndex(options);
    var csv = SeriesInventory.ToCsv(index);
    if (options.Single("--out") is string outPath)
    {
        File.WriteAllText(outPath, csv);
        Console.WriteLine($"Wrote {index.Series.Count} series to {outPath}.");
    }
    else
    {
        Console.Write(csv);
    }
    return 0;
}

static int Extract(Options options)
{
    var index = LoadIndex(options);
    var outDir = options.Required("--out");
    var gzip = options.Flag("--gzip");

    var seriesIds = options.Flag("--all")
        ? index.Series.Keys.Where(id => index.SeriesWithDescendants(id).Any(s => index.RacesBySeries[s].Any())).ToList()
        : options.All("--series").Select(selector => FindSeries(index, selector)).Distinct().ToList();
    if (seriesIds.Count == 0)
    {
        throw new ArgumentException("Pass --series (one or more) or --all.");
    }

    Directory.CreateDirectory(outDir);
    var extractor = new FixtureExtractor(index);
    var initials = Json.String(index.Root, "initials") ?? "club";
    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var seriesId in seriesIds)
    {
        var series = index.Series[seriesId];
        var (fixture, warnings) = extractor.Extract(seriesId);
        var baseName = FileName(initials, index.SeasonUrlName(series), Json.String(series, "urlName") ?? seriesId.ToString());
        if (!usedNames.Add(baseName))
        {
            // Old data has a few series sharing a season and URL name.
            baseName += "_" + seriesId.ToString()[..8];
            usedNames.Add(baseName);
        }
        var fileName = baseName + (gzip ? ".json.gz" : ".json");
        Json.WriteFile(Path.Combine(outDir, fileName), fixture, gzip);

        Console.WriteLine($"{fileName}: {Json.Count(fixture, "races")} races, "
            + $"{Json.Count(fixture, "competitors")} competitors");
        foreach (var warning in warnings)
        {
            Console.WriteLine($"  warning: {warning}");
        }
    }
    return 0;
}

static int Anonymize(string[] paths)
{
    if (paths.Length == 0)
    {
        throw new ArgumentException("Pass one or more fixture files.");
    }
    foreach (var path in paths)
    {
        var fixture = Json.ReadFile(path);
        var renamed = NameAnonymizer.Apply(fixture,
            NameAnonymizer.BuildNames(Json.Objects(fixture, "competitors")));
        if (renamed > 0)
        {
            Json.WriteFile(path, fixture, gzip: path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase));
        }
        Console.WriteLine($"{Path.GetFileName(path)}: {renamed} changes (competitors renamed, race descriptions removed)");
    }
    return 0;
}

static BackupIndex LoadIndex(Options options)
{
    var backupPath = options.Positional ?? throw new ArgumentException("Missing backup file.");
    var shared = options.Single("--shared") is string sharedPath ? Json.ReadFile(sharedPath) : null;
    return new BackupIndex(Json.ReadFile(backupPath), shared);
}

static Guid FindSeries(BackupIndex index, string selector)
{
    if (Guid.TryParse(selector, out var id))
    {
        return index.Series.ContainsKey(id) ? id : throw new ArgumentException($"No series with id {selector}.");
    }

    var parts = selector.Split('/', 2);
    var (season, urlName) = parts.Length == 2 ? (parts[0], parts[1]) : (null, parts[0]);
    var matches = index.Series.Values
        .Where(s => string.Equals(Json.String(s, "urlName"), urlName, StringComparison.OrdinalIgnoreCase)
            && (season is null || string.Equals(index.SeasonUrlName(s), season, StringComparison.OrdinalIgnoreCase)))
        .ToList();
    return matches.Count switch
    {
        1 => Json.Guid(matches[0], "id")!.Value,
        0 => throw new ArgumentException($"No series matches '{selector}'."),
        _ => throw new ArgumentException($"'{selector}' matches several series; use season/urlName: "
            + string.Join(", ", matches.Select(s => $"{index.SeasonUrlName(s)}/{Json.String(s, "urlName")}")))
    };
}

static string FileName(params string[] parts) =>
    string.Join("_", parts.Where(p => p.Length > 0)
        .Select(p => Regex.Replace(p.ToLowerInvariant(), "[^a-z0-9-]+", "-").Trim('-')));

// Minimal parsing: one positional argument, "--name value" options (repeatable) and flags.
internal sealed class Options
{
    private static readonly HashSet<string> Flags = ["--all", "--gzip"];
    private readonly List<(string Name, string Value)> _values = [];
    private readonly HashSet<string> _flags = [];

    public string? Positional { get; }

    public Options(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (Flags.Contains(arg))
            {
                _flags.Add(arg);
            }
            else if (arg.StartsWith("--"))
            {
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException($"{arg} needs a value.");
                }
                _values.Add((arg, args[++i]));
            }
            else if (Positional is null)
            {
                Positional = arg;
            }
            else
            {
                throw new ArgumentException($"Unexpected argument '{arg}'.");
            }
        }
    }

    public bool Flag(string name) => _flags.Contains(name);

    public IEnumerable<string> All(string name) => _values.Where(v => v.Name == name).Select(v => v.Value);

    public string? Single(string name) => All(name).LastOrDefault();

    public string Required(string name) => Single(name) ?? throw new ArgumentException($"Missing {name}.");
}
