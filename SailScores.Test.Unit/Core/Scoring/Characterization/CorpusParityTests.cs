using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring.Characterization;

// Opt-in, local-only parity check over a large corpus of fixtures: every series you have
// extracted from club backups, too many (and too private) to commit.
//
// Set FIXTURE_CORPUS_DIR to a folder of fixtures (searched recursively). The first run writes
// each series' results to <corpus>/.baseline/<relative path>.json. Later runs compare with
// that baseline and fail listing every series whose results changed, writing a
// .received.json beside each baseline file. Delete .baseline to start again, for example
// after an approved behaviour change.
//
// Run it before a refactoring step to make the baseline (or keep the one from main), then
// after the step:
//   $env:FIXTURE_CORPUS_DIR = "D:\fixtures"
//   dotnet test --project SailScores.Test.Unit/SailScores.Test.Unit.csproj --filter-class "*CorpusParityTests"
public class CorpusParityTests
{
    private const string CorpusVariable = "FIXTURE_CORPUS_DIR";
    private const string BaselineFolder = ".baseline";

    [Fact]
    public async Task Score_EveryCorpusFixture_MatchesBaseline()
    {
        var corpus = Environment.GetEnvironmentVariable(CorpusVariable);
        Assert.SkipWhen(String.IsNullOrWhiteSpace(corpus), $"Set {CorpusVariable} to run the corpus parity check.");
        Assert.True(Directory.Exists(corpus), $"{CorpusVariable} folder not found: {corpus}");

        var baselineRoot = Path.Combine(corpus, BaselineFolder);
        var files = Directory.EnumerateFiles(corpus, "*.json*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".json") || f.EndsWith(".json.gz"))
                && !Path.GetRelativePath(corpus, f).StartsWith(BaselineFolder))
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.NotEmpty(files);

        var created = new ConcurrentBag<string>();
        var changed = new ConcurrentBag<string>();
        await Parallel.ForEachAsync(
            files,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            async (file, _) =>
            {
                var relative = Path.GetRelativePath(corpus, file);
                var name = Path.Combine(Path.GetDirectoryName(relative)!, ScoringFixture.NameOf(Path.GetFileName(file)));
                var actual = Normalize(await ScoreAsync(file));
                var baselinePath = Path.Combine(baselineRoot, name + ".json");
                var receivedPath = Path.Combine(baselineRoot, name + ".received.json");
                Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);

                if (!File.Exists(baselinePath))
                {
                    await File.WriteAllTextAsync(baselinePath, actual);
                    created.Add(name);
                }
                else if (Normalize(await File.ReadAllTextAsync(baselinePath)) != actual)
                {
                    await File.WriteAllTextAsync(receivedPath, actual);
                    changed.Add(name);
                }
                else
                {
                    File.Delete(receivedPath);
                }
            });

        TestContext.Current.SendDiagnosticMessage(
            $"Corpus: {files.Count} fixtures, {created.Count} new baselines, {changed.Count} changed.");
        Assert.True(changed.IsEmpty,
            $"Scoring output changed for {changed.Count} of {files.Count} corpus fixtures. "
            + $"Diff each .received.json against its baseline in {baselineRoot}:\n  "
            + String.Join("\n  ", changed.Order(StringComparer.Ordinal)));
    }

    // A fixture that fails to score is recorded too, so a change in what throws shows up.
    private static async Task<string> ScoreAsync(string file)
    {
        try
        {
            var scored = await FixtureScorer.ScoreAsync(ScoringFixture.LoadFile(file));
            return ResultsSnapshot.ToJson(scored.Results);
        }
        catch (Exception ex)
        {
            return $"ERROR {ex.GetType().Name}: {ex.Message}";
        }
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd() + "\n";
}
