using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SailScores.Core.FlatModel;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring.Characterization;

// Opt-in, local-only release check: rescores every series in a SQL Server database (a
// restored copy of production) and compares with a baseline made by another version of
// the code. See FeaturePlans/Scoring-Release-Plan.md.
//
//   PROD_PARITY_CONNECTION  connection string to the local copy. Never point it at prod.
//   PROD_PARITY_DIR         output folder, outside the repository (results hold real names).
//   PROD_PARITY_PARALLEL    optional: clubs scored at once (default 1).
//
// The first run writes <dir>/.baseline/<club>/<series>.txt; later runs compare with it and
// fail listing every series whose results changed, writing .received.txt beside each one.
// Make the baseline with main, then run this branch (commands in the release plan):
//   1. In a worktree of main, copy this file into the same folder (main doesn't have the
//      folder yet: create it) and build the test
//      project with -p:TypeScriptCompileBlocked=true (a fresh worktree has no node_modules).
//   2. Run main's test executable directly; its dotnet test uses VSTest, which the .NET 10
//      SDK refuses for this project:
//        SailScores.Test.Unit\bin\Debug\net10.0\SailScores.Test.Unit.exe -class SailScores.Test.Unit.Core.Scoring.Characterization.ProdParityTests
//   3. Run it here with the same settings:
//        dotnet test --project SailScores.Test.Unit/SailScores.Test.Unit.csproj --filter-class "*ProdParityTests"
//
// Each club is scored inside a transaction that is rolled back, so the database is left as
// it was and both runs score the same data. Only fields both versions have are compared,
// so this file compiles against main too: keep it that way.
public class ProdParityTests
{
    private const string ConnectionVariable = "PROD_PARITY_CONNECTION";
    private const string DirectoryVariable = "PROD_PARITY_DIR";
    private const string ParallelVariable = "PROD_PARITY_PARALLEL";
    private const string BaselineFolder = ".baseline";

    [Fact]
    public async Task Score_EverySeriesInDatabase_MatchesBaseline()
    {
        var connection = Environment.GetEnvironmentVariable(ConnectionVariable);
        var outputDir = Environment.GetEnvironmentVariable(DirectoryVariable);
        Assert.SkipWhen(String.IsNullOrWhiteSpace(connection) || String.IsNullOrWhiteSpace(outputDir),
            $"Set {ConnectionVariable} and {DirectoryVariable} to run the production parity check.");
        var parallel = Int32.TryParse(Environment.GetEnvironmentVariable(ParallelVariable), out var p) && p > 0 ? p : 1;

        var options = new DbContextOptionsBuilder<SailScoresContext>()
            .UseSqlServer(connection, sql => sql.CommandTimeout(600))
            .Options;
        List<(Guid Id, string Initials)> clubs;
        await using (var context = new SailScoresContext(options))
        {
            clubs = (await context.Clubs.Select(c => new { c.Id, c.Initials }).ToListAsync())
                .Select(c => (c.Id, c.Initials ?? c.Id.ToString()))
                .OrderBy(c => c.Item2, StringComparer.Ordinal)
                .ToList();
        }
        Assert.NotEmpty(clubs);

        var baselineRoot = Path.Combine(outputDir, BaselineFolder);
        var created = new ConcurrentBag<string>();
        var changed = new ConcurrentBag<string>();
        var seriesCount = 0;
        await Parallel.ForEachAsync(
            clubs,
            new ParallelOptions { MaxDegreeOfParallelism = parallel },
            async (club, _) =>
            {
                foreach (var (name, text) in await ScoreClubAsync(options, club.Id, club.Initials))
                {
                    System.Threading.Interlocked.Increment(ref seriesCount);
                    var baselinePath = Path.Combine(baselineRoot, name + ".txt");
                    var receivedPath = Path.Combine(baselineRoot, name + ".received.txt");
                    Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
                    if (!File.Exists(baselinePath))
                    {
                        await File.WriteAllTextAsync(baselinePath, text);
                        created.Add(name);
                    }
                    else if (await File.ReadAllTextAsync(baselinePath) != text)
                    {
                        await File.WriteAllTextAsync(receivedPath, text);
                        changed.Add(name);
                    }
                    else
                    {
                        File.Delete(receivedPath);
                    }
                }
            });

        var summary = $"Production parity: {clubs.Count} clubs, {seriesCount} series, "
            + $"{created.Count} new baselines, {changed.Count} changed.";
        await File.WriteAllTextAsync(Path.Combine(outputDir, "changed.txt"),
            summary + "\n" + String.Join("\n", changed.Order(StringComparer.Ordinal)) + "\n");
        TestContext.Current.SendDiagnosticMessage(summary);
        Assert.True(changed.IsEmpty,
            $"{summary} Diff each .received.txt against its baseline in {baselineRoot} "
            + $"(list in {Path.Combine(outputDir, "changed.txt")}).");
    }

    // Scores every series of one club, child series before their parents, the way the site
    // does after a race changes, then rolls back. Returns one rendering per series.
    private static async Task<List<(string Name, string Text)>> ScoreClubAsync(
        DbContextOptions<SailScoresContext> options, Guid clubId, string initials)
    {
        await using var context = new SailScoresContext(options);
        await using var transaction = await context.Database.BeginTransactionAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var mapper = MapperBuilder.GetSailScoresMapper();
        var seriesService = new SeriesService(
            new ScoringCalculatorFactory(context, cache),
            new ScoringService(context, cache, mapper),
            new HandicapService(context, mapper),
            new Mock<IForwarderService>().Object,
            new Mock<IConversionService>().Object,
            new DbObjectBuilder(context, mapper),
            context,
            cache,
            mapper,
            new Mock<IIndexNowService>().Object);

        var series = await context.Series
            .Where(s => s.ClubId == clubId)
            .Select(s => new { s.Id, s.UrlName, Season = s.Season.UrlName, s.ResultsLocked })
            .ToListAsync();
        var children = (await context.SeriesToSeriesLinks
                .Where(l => series.Select(s => s.Id).Contains(l.ParentSeriesId))
                .Select(l => new { l.ParentSeriesId, l.ChildSeriesId })
                .ToListAsync())
            .ToLookup(l => l.ParentSeriesId, l => l.ChildSeriesId);

        var outcomes = new Dictionary<Guid, string>();
        async Task ScoreAsync(Guid seriesId, HashSet<Guid> path)
        {
            if (outcomes.ContainsKey(seriesId) || !path.Add(seriesId))
            {
                return;
            }
            foreach (var childId in children[seriesId])
            {
                await ScoreAsync(childId, path);
            }
            try
            {
                await seriesService.UpdateSeriesResults(seriesId, "prod-parity", calculateParents: false);
                outcomes[seriesId] = null;
            }
            catch (Exception ex)
            {
                outcomes[seriesId] = $"ERROR {ex.GetType().Name}: {ex.Message}";
                context.ChangeTracker.Clear();
            }
            path.Remove(seriesId);
        }

        var renderings = new List<(string Name, string Text)>();
        foreach (var s in series.OrderBy(s => s.Season).ThenBy(s => s.UrlName).ThenBy(s => s.Id))
        {
            await ScoreAsync(s.Id, new HashSet<Guid>());
            var json = await context.HistoricalResults
                .Where(h => h.SeriesId == s.Id && h.IsCurrent)
                .Select(h => h.Results)
                .FirstOrDefaultAsync();
            var header = s.ResultsLocked == true ? "LOCKED (stored results, not rescored)\n" : "";
            var text = outcomes.GetValueOrDefault(s.Id) is string error
                ? header + error + "\n"
                : header + Render(json == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<FlatResults>(json));
            renderings.Add(($"{Safe(initials)}/{Safe(s.Season)}__{Safe(s.UrlName)}__{s.Id.ToString()[..8]}", text));
        }

        await transaction.RollbackAsync();
        return renderings;
    }

    // One line per competitor in rank order: rank, id, total, total with discards, average,
    // participation, trend, then each race's code, place, points and discard.
    private static string Render(FlatResults results)
    {
        if (results?.CalculatedScores == null)
        {
            return "NO RESULTS\n";
        }
        var raceOrder = (results.Races ?? Enumerable.Empty<FlatRace>()).Select(r => r.Id).ToList();
        var sb = new StringBuilder();
        sb.Append("Races ").Append(raceOrder.Count)
            .Append(" Discards ").Append(results.NumberOfDiscards)
            .Append(" System ").Append(results.ScoringSystemName).Append('\n');
        foreach (var score in results.CalculatedScores
            .OrderBy(s => s.Rank ?? Int32.MaxValue)
            .ThenBy(s => s.CompetitorId))
        {
            var byRace = (score.Scores ?? Enumerable.Empty<FlatCalculatedScore>()).ToDictionary(c => c.RaceId);
            sb.Append(score.Rank).Append(" | ").Append(score.CompetitorId)
                .Append(" | ").Append(N(score.TotalScore))
                .Append(" | ").Append(N(score.TotalScoreIncludingDiscards))
                .Append(" | ").Append(N(score.Average))
                .Append(" | ").Append(N(score.ParticipationPercent))
                .Append(" | ").Append(score.Trend)
                .Append(" |");
            foreach (var raceId in raceOrder)
            {
                sb.Append(' ');
                if (!byRace.TryGetValue(raceId, out var c))
                {
                    sb.Append('-');
                    continue;
                }
                sb.Append(c.Discard ? "(" : "")
                    .Append(c.Code).Append(c.Code == null ? "" : " ")
                    .Append(c.Place).Append(':').Append(N(c.ScoreValue))
                    .Append(c.Discard ? ")" : "");
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }

    private static string N(decimal? value) =>
        value?.ToString("0.##########", CultureInfo.InvariantCulture) ?? "";

    private static string Safe(string text) =>
        Regex.Replace(text ?? "none", "[^A-Za-z0-9._-]+", "-");
}
