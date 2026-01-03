using System.Text.Json;
using SailScores.DocScreenshots.Models;
using SailScores.DocScreenshots.Services;

Console.WriteLine("SailScores Doc Screenshots - starting");

EnsurePagesFileExists();
var pages = GetPages();
await using var service = await ScreenshotService.CreateAsync();
var outputDir = Path.Combine(AppContext.BaseDirectory, "screenshots");
Directory.CreateDirectory(outputDir);

await CaptureScreenshotsAsync(pages, service, outputDir);

Console.WriteLine("Capturing images done.");

await PromptAndCopyScreenshotsAsync(outputDir);

static void EnsurePagesFileExists()
{
    var pagesPath = Path.Combine(AppContext.BaseDirectory, "pages.json");
    if (!File.Exists(pagesPath))
    {
        Console.WriteLine($"pages.json not found at {pagesPath}. Creating default pages.json.");
        var defaultPages = new[]
        {
            new PageInfo { Name = "Home", Url = "http://localhost:5000/" },
            new PageInfo { Name = "Help", Url = "http://localhost:5000/Help" },
            new PageInfo { Name = "SeriesDetails", Url = "http://localhost:5000/Series/Details/1" }
        };
        var json = JsonSerializer.Serialize(defaultPages, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(pagesPath, json);
    }
}

static PageInfo[] GetPages()
{
    var pagesPath = Path.Combine(AppContext.BaseDirectory, "pages.json");
    var pagesJson = File.ReadAllText(pagesPath);
    return JsonSerializer.Deserialize<PageInfo[]>(pagesJson) ?? Array.Empty<PageInfo>();
}

static async Task CaptureScreenshotsAsync(PageInfo[] pages, ScreenshotService service, string outputDir)
{
    foreach (var p in pages)
    {
        Console.WriteLine($"Capturing {p.Name} - {p.Url}");
        await service.CaptureAsync(p, outputDir);
    }
}

static async Task PromptAndCopyScreenshotsAsync(string outputDir)
{
    // Prompt user to optionally copy screenshots to SailScores.Web/wwwroot/images/help/
    Console.Write("Copy screenshots to SailScores.Web/wwwroot/images/help/? (y/N): ");
    var answer = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(answer) && (answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) || answer.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase)))
    {
        var targetDir = GetTargetDir();
        if (!string.IsNullOrWhiteSpace(targetDir))
        {
            CopyScreenshots(outputDir, targetDir);
        }
        else
        {
            Console.WriteLine("Copy cancelled.");
        }
    }
    else
    {
        Console.WriteLine("Skipping copy.");
    }
}

static string? GetTargetDir()
{
    // Try to locate the repository root by finding a directory that contains SailScores.Web
    var dirInfo = new DirectoryInfo(AppContext.BaseDirectory);
    DirectoryInfo? repoRoot = null;
    while (dirInfo != null)
    {
        var candidate = Path.Combine(dirInfo.FullName, "SailScores.Web");
        if (Directory.Exists(candidate))
        {
            repoRoot = dirInfo;
            break;
        }
        dirInfo = dirInfo.Parent;
    }

    string? targetDir = null;
    if (repoRoot != null)
    {
        targetDir = Path.Combine(repoRoot.FullName, "SailScores.Web", "wwwroot", "images", "help");
    }
    else
    {
        Console.WriteLine("Could not locate SailScores.Web automatically.");
        Console.Write("Enter target directory path to copy screenshots to (or blank to cancel): ");
        var manual = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(manual))
            targetDir = manual.Trim();
    }
    return targetDir;
}

static void CopyScreenshots(string outputDir, string targetDir)
{
    try
    {
        Directory.CreateDirectory(targetDir);
        var files = Directory.Exists(outputDir) ? Directory.EnumerateFiles(outputDir) : Array.Empty<string>();
        foreach (var file in files)
        {
            var dest = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
            Console.WriteLine($"Copied {Path.GetFileName(file)} -> {dest}");
        }

        Console.WriteLine($"Screenshots copied to: {targetDir}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to copy screenshots: {ex.Message}");
    }
}
