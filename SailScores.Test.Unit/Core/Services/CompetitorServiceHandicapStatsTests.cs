using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using SailScores.Core.Services;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// Explicit aliases to resolve namespace clashes between Database.Entities and Core.Model.
using CoreClub = SailScores.Core.Model.Club;
using CoreHandicapSystem = SailScores.Core.Model.HandicapSystem;
using CoreHandicapSystemType = SailScores.Core.Model.HandicapSystemType;
using DbClassHandicap = SailScores.Database.Entities.ClassHandicap;
using DbCompetitor = SailScores.Database.Entities.Competitor;
using DbCompetitorHandicap = SailScores.Database.Entities.CompetitorHandicap;
using DbCompetitorStatsSummary = SailScores.Database.Entities.CompetitorStatsSummary;
using DbCompetitorHandicapStatsSummary = SailScores.Database.Entities.CompetitorHandicapStatsSummary;
using IClubService = SailScores.Core.Services.IClubService;
using IForwarderService = SailScores.Core.Services.IForwarderService;

namespace SailScores.Test.Unit.Core.Services
{
    /// <summary>
    /// Tests for the handicap-aware competitor stats path in CompetitorService.
    /// Uses a mocked ISailScoresContext so stored-procedure calls can be substituted.
    /// </summary>
    public class CompetitorServiceHandicapStatsTests
    {
        private static readonly Guid ClubId = Guid.NewGuid();
        private static readonly Guid SystemId = Guid.NewGuid();
        private static readonly Guid CompetitorId = Guid.NewGuid();
        private static readonly Guid BoatClassId = Guid.NewGuid();

        private static CoreClub BuildClubWithHandicap(bool enableHandicap = true, bool hasDefaultSystem = true)
        {
            var system = hasDefaultSystem
                ? new CoreHandicapSystem { Id = SystemId, Name = "PHRF ToT", SystemType = CoreHandicapSystemType.PhrfToT }
                : null;

            return new CoreClub
            {
                Id = ClubId,
                Name = "Test Club",
                EnableHandicapScoring = enableHandicap,
                DefaultHandicapSystemId = hasDefaultSystem ? SystemId : (Guid?)null,
                DefaultHandicapSystem = system
            };
        }

        private static DbCompetitor BuildCompetitorEntity(bool hasRating)
        {
            var handicaps = hasRating
                ? new List<DbCompetitorHandicap>
                {
                    new DbCompetitorHandicap { Id = Guid.NewGuid(), CompetitorId = CompetitorId,
                        HandicapSystemId = SystemId, Value = 150m }
                }
                : new List<DbCompetitorHandicap>();

            return new DbCompetitor
            {
                Id = CompetitorId,
                ClubId = ClubId,
                BoatClassId = BoatClassId,
                Handicaps = handicaps
            };
        }

        private static DbCompetitorStatsSummary BuildRawSummary(string seasonUrl = "2024") =>
            new DbCompetitorStatsSummary
            {
                SeasonName = seasonUrl, SeasonUrlName = seasonUrl,
                SeasonStart = new DateTime(2024, 1, 1), SeasonEnd = new DateTime(2024, 12, 31),
                RaceCount = 10, DaysRaced = 5
            };

        private static DbCompetitorHandicapStatsSummary BuildCorrectedSummary(string seasonUrl = "2024") =>
            new DbCompetitorHandicapStatsSummary
            {
                SeasonUrlName = seasonUrl,
                CorrectedRaceCount = 8, AverageCorrectedRank = 3.5, CorrectedBoatsRacedAgainst = 9
            };

        private Mock<ISailScoresContext> BuildContextMock(
            DbCompetitor competitorEntity,
            List<DbClassHandicap> classHandicaps,
            List<DbCompetitorStatsSummary> rawSummaries,
            List<DbCompetitorHandicapStatsSummary> correctedSummaries = null)
        {
            var mock = new Mock<ISailScoresContext>();

            mock.Setup(c => c.GetCompetitorStatsSummaryAsync(ClubId, CompetitorId))
                .ReturnsAsync(rawSummaries);

            mock.Setup(c => c.GetCompetitorHandicapStatsSummaryAsync(
                    ClubId, CompetitorId, SystemId, It.IsAny<int>()))
                .ReturnsAsync(correctedSummaries ?? new List<DbCompetitorHandicapStatsSummary>());

            mock.Setup(c => c.Competitors)
                .Returns(MockDbSet(new List<DbCompetitor> { competitorEntity }).Object);

            mock.Setup(c => c.ClassHandicaps)
                .Returns(MockDbSet(classHandicaps).Object);

            return mock;
        }

        private SailScores.Core.Services.CompetitorService BuildService(
            Mock<ISailScoresContext> contextMock, CoreClub club)
        {
            var clubServiceMock = new Mock<IClubService>();
            clubServiceMock.Setup(s => s.GetFullClubExceptScores(ClubId)).ReturnsAsync(club);

            return new SailScores.Core.Services.CompetitorService(
                contextMock.Object,
                new Mock<IForwarderService>().Object,
                Utilities.MapperBuilder.GetSailScoresMapper(),
                clubServiceMock.Object,
                new Mock<IConversionService>().Object);
        }

        // ---- tests ----

        [Fact]
        public async Task GetCompetitorStatsWithHandicapAsync_ClubHasNoHandicapScoring_ReturnsNoCorrections()
        {
            var ctx = BuildContextMock(BuildCompetitorEntity(false), new List<DbClassHandicap>(),
                new List<DbCompetitorStatsSummary> { BuildRawSummary() });

            var result = await BuildService(ctx, BuildClubWithHandicap(enableHandicap: false))
                .GetCompetitorStatsWithHandicapAsync(ClubId, CompetitorId);

            Assert.False(result.ClubHasHandicapScoring);
            Assert.All(result.SeasonStats, s => Assert.Null(s.CorrectedRaceCount));
        }

        [Fact]
        public async Task GetCompetitorStatsWithHandicapAsync_NoDefaultSystem_ReturnsNoCorrections()
        {
            var ctx = BuildContextMock(BuildCompetitorEntity(false), new List<DbClassHandicap>(),
                new List<DbCompetitorStatsSummary> { BuildRawSummary() });

            var result = await BuildService(ctx, BuildClubWithHandicap(enableHandicap: true, hasDefaultSystem: false))
                .GetCompetitorStatsWithHandicapAsync(ClubId, CompetitorId);

            Assert.True(result.ClubHasHandicapScoring);
            Assert.False(result.ClubHasDefaultHandicapSystem);
            Assert.All(result.SeasonStats, s => Assert.Null(s.CorrectedRaceCount));
        }

        [Fact]
        public async Task GetCompetitorStatsWithHandicapAsync_IndividualRating_MergesCorrectedStats()
        {
            var ctx = BuildContextMock(BuildCompetitorEntity(hasRating: true), new List<DbClassHandicap>(),
                new List<DbCompetitorStatsSummary> { BuildRawSummary() },
                new List<DbCompetitorHandicapStatsSummary> { BuildCorrectedSummary() });

            var result = await BuildService(ctx, BuildClubWithHandicap())
                .GetCompetitorStatsWithHandicapAsync(ClubId, CompetitorId);

            Assert.True(result.CompetitorHasRatingForDefaultSystem);
            Assert.Equal(8, result.SeasonStats.Single().CorrectedRaceCount);
            Assert.Equal(3.5, result.SeasonStats.Single().AverageCorrectedRank);
        }

        [Fact]
        public async Task GetCompetitorStatsWithHandicapAsync_ClassRatingFallback_MergesCorrectedStats()
        {
            var classHandicap = new DbClassHandicap
            {
                Id = Guid.NewGuid(), BoatClassId = BoatClassId, HandicapSystemId = SystemId, Value = 120m
            };
            var ctx = BuildContextMock(BuildCompetitorEntity(hasRating: false),
                new List<DbClassHandicap> { classHandicap },
                new List<DbCompetitorStatsSummary> { BuildRawSummary() },
                new List<DbCompetitorHandicapStatsSummary> { BuildCorrectedSummary() });

            var result = await BuildService(ctx, BuildClubWithHandicap())
                .GetCompetitorStatsWithHandicapAsync(ClubId, CompetitorId);

            Assert.True(result.CompetitorHasRatingForDefaultSystem);
            Assert.Equal(8, result.SeasonStats.Single().CorrectedRaceCount);
        }

        [Fact]
        public async Task GetCompetitorStatsWithHandicapAsync_NeitherRatingPresent_SpNotCalled()
        {
            var ctx = BuildContextMock(BuildCompetitorEntity(hasRating: false), new List<DbClassHandicap>(),
                new List<DbCompetitorStatsSummary> { BuildRawSummary() });

            var result = await BuildService(ctx, BuildClubWithHandicap())
                .GetCompetitorStatsWithHandicapAsync(ClubId, CompetitorId);

            Assert.False(result.CompetitorHasRatingForDefaultSystem);
            Assert.All(result.SeasonStats, s => Assert.Null(s.CorrectedRaceCount));
            ctx.Verify(c => c.GetCompetitorHandicapStatsSummaryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetCompetitorStatsWithHandicapAsync_PartialCoverage_OnlyMatchedSeasonsUpdated()
        {
            var ctx = BuildContextMock(BuildCompetitorEntity(hasRating: true), new List<DbClassHandicap>(),
                new List<DbCompetitorStatsSummary> { BuildRawSummary("2023"), BuildRawSummary("2024") },
                new List<DbCompetitorHandicapStatsSummary> { BuildCorrectedSummary("2024") });

            var result = await BuildService(ctx, BuildClubWithHandicap())
                .GetCompetitorStatsWithHandicapAsync(ClubId, CompetitorId);

            Assert.Null(result.SeasonStats.Single(s => s.SeasonUrlName == "2023").CorrectedRaceCount);
            Assert.Equal(8, result.SeasonStats.Single(s => s.SeasonUrlName == "2024").CorrectedRaceCount);
        }

        // ---- async DbSet mock infrastructure ----

        private static Mock<DbSet<T>> MockDbSet<T>(List<T> data) where T : class
        {
            var q = data.AsQueryable();
            var mock = new Mock<DbSet<T>>();
            mock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(q.GetEnumerator()));
            mock.As<IQueryable<T>>().Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(q.Provider));
            mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(q.Expression);
            mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(q.ElementType);
            mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(q.GetEnumerator());
            return mock;
        }

        private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;
            public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;
            public IQueryable CreateQuery(Expression e) => new TestAsyncEnumerable<TEntity>(e);
            public IQueryable<TElement> CreateQuery<TElement>(Expression e) => new TestAsyncEnumerable<TElement>(e);
            public object Execute(Expression e) => _inner.Execute(e);
            public TResult Execute<TResult>(Expression e) => _inner.Execute<TResult>(e);
            public TResult ExecuteAsync<TResult>(Expression e, CancellationToken ct = default)
            {
                var itemType = typeof(TResult).GetGenericArguments()[0];
                var executeGeneric = typeof(IQueryProvider)
                    .GetMethods()
                    .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                    .MakeGenericMethod(itemType);
                var value = executeGeneric.Invoke(_inner, new object[] { e });
                return (TResult)typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(itemType)
                    .Invoke(null, new[] { value });
            }
        }

        private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> items) : base(items) { }
            public TestAsyncEnumerable(Expression e) : base(e) { }
            IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default) =>
                new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;
            public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
            public T Current => _inner.Current;
            public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
            public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
        }
    }
}
