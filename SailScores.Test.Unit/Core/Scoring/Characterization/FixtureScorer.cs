using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;

namespace SailScores.Test.Unit.Core.Scoring.Characterization;

public sealed record ScoredFixture(
    ScoringFixture Fixture,
    FlatResults Results,
    ScoringCalculatorType? CalculatorType,
    bool IsSummarySeries);

// Restores a fixture into an in-memory database and scores its series the way the site
// does after a race changes: through SeriesService, child series first.
public static class FixtureScorer
{
    // Snapshot and invariant tests share each scoring run.
    private static readonly ConcurrentDictionary<string, Lazy<Task<ScoredFixture>>> Cache = new();

    public static Task<ScoredFixture> ScoreAsync(string fixtureName, string sharedSystemName = null) =>
        Cache.GetOrAdd(
            $"{fixtureName}|{sharedSystemName}",
            _ => new Lazy<Task<ScoredFixture>>(() => ScoreUncachedAsync(fixtureName, sharedSystemName))).Value;

    public static Task<ScoredFixture> ScoreUncachedAsync(string fixtureName, string sharedSystemName = null)
    {
        var fixture = ScoringFixture.Load(fixtureName);
        if (sharedSystemName != null)
        {
            fixture.UseSharedSystem(sharedSystemName);
        }
        return ScoreAsync(fixture);
    }

    public static async Task<ScoredFixture> ScoreAsync(ScoringFixture fixture)
    {
        var context = InMemoryContextBuilder.GetContext();
        var clubId = (await context.Clubs.FirstAsync()).Id;
        await new BackupService(context, NullLogger<BackupService>.Instance)
            .RestoreBackupAsync(clubId, fixture.Backup);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var mapper = MapperBuilder.GetSailScoresMapper();
        var seriesService = new SeriesService(
            new ScoringCalculatorFactory(context, cache),
            new ScoringService(context, cache, mapper),
            new Mock<IHandicapService>().Object,
            new Mock<IForwarderService>().Object,
            new Mock<IConversionService>().Object,
            new DbObjectBuilder(context, mapper),
            context,
            cache,
            mapper,
            new Mock<IIndexNowService>().Object);

        var series = await context.Series
            .SingleAsync(s => s.ClubId == clubId
                && s.Name == fixture.SeriesName
                && s.UrlName == fixture.SeriesUrlName
                && s.Season.UrlName == fixture.SeasonUrlName);
        var isSummary = await context.SeriesToSeriesLinks.AnyAsync(l => l.ParentSeriesId == series.Id);
        await ScoreWithChildrenAsync(context, seriesService, series.Id);

        var json = await context.HistoricalResults
            .Where(h => h.SeriesId == series.Id && h.IsCurrent)
            .Select(h => h.Results)
            .SingleAsync();
        var results = Newtonsoft.Json.JsonConvert.DeserializeObject<FlatResults>(json);

        return new ScoredFixture(fixture, results, await CalculatorTypeAsync(context, clubId, results), isSummary);
    }

    private static async Task ScoreWithChildrenAsync(ISailScoresContext context, SeriesService seriesService, Guid seriesId)
    {
        var childIds = await context.SeriesToSeriesLinks
            .Where(l => l.ParentSeriesId == seriesId)
            .Select(l => l.ChildSeriesId)
            .ToListAsync();
        foreach (var childId in childIds)
        {
            await ScoreWithChildrenAsync(context, seriesService, childId);
        }
        await seriesService.UpdateSeriesResults(seriesId, "fixture-test", calculateParents: false);
    }

    // The calculator comes from the root of the scoring system's parent chain.
    private static async Task<ScoringCalculatorType?> CalculatorTypeAsync(ISailScoresContext context, Guid clubId, FlatResults results)
    {
        var system = await context.ScoringSystems
            .FirstOrDefaultAsync(s => s.ClubId == clubId && s.Name == results.ScoringSystemName);
        while (system?.ParentSystemId != null)
        {
            system = await context.ScoringSystems.FindAsync(system.ParentSystemId.Value);
        }
        return system?.CalculatorType;
    }
}
