using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SailScores.Core.FlatModel;
using SailScores.Core.Mapping;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services;

public class SeriesServiceTests
{
    private readonly Series _fakeSeries;
    private readonly DbObjectBuilder _dbObjectBuilder;
    private readonly SeriesService _service;
    private readonly Mock<IScoringCalculator> _mockCalculator;
    private readonly Mock<IScoringCalculatorFactory> _mockScoringCalculatorFactory;
    private readonly IMapper _mapper;
    private readonly ISailScoresContext _context;
    private readonly string _clubInitials;
    private readonly string _seriesUrlName;
    private readonly Mock<IScoringService> _mockScoringService;
    private readonly Mock<IForwarderService> _mockForwarderService;
    private readonly Mock<IConversionService> _mockConversionService;
    private readonly Mock<IIndexNowService> _mockIndexNowService;
    private readonly Mock<IHandicapService> _mockHandicapService;
    private readonly IMemoryCache _realCache;

    public SeriesServiceTests()
    {
        _mockCalculator = new Mock<IScoringCalculator>();
        _mockCalculator.Setup(c => c.CalculateResults(It.IsAny<Series>())).Returns(new SeriesResults
        {
            Results = new Dictionary<Competitor, SeriesCompetitorResults>()
        });
        _mockScoringCalculatorFactory = new Mock<IScoringCalculatorFactory>();
        _mockScoringCalculatorFactory.Setup(f => f.CreateScoringCalculatorAsync(It.IsAny<SailScores.Core.Model.ScoringSystem>()))
            .ReturnsAsync(_mockCalculator.Object);
        _mockScoringService = new Mock<IScoringService>();
        _mockForwarderService = new Mock<IForwarderService>();
        _mockConversionService = new Mock<IConversionService>();
        _mockIndexNowService = new Mock<IIndexNowService>();
        _mockHandicapService = new Mock<IHandicapService>();
        _realCache = new MemoryCache(new MemoryCacheOptions());

        _context = Utilities.InMemoryContextBuilder.GetContext();
        _clubInitials = _context.Clubs.First().Initials;

        var _clubId = _context.Clubs.First().Id;


        var config = new MapperConfiguration(opts =>
        {
            opts.AddProfile(new DbToModelMappingProfile());
        });

        _mapper = config.CreateMapper();

        _fakeSeries = _mapper.Map<Series>(_context.Series.Where(s => s.ClubId == _clubId).First());

        _seriesUrlName = _fakeSeries.UrlName;

        //yep, this means we are testing the real DbObjectBuilder as well:
        _dbObjectBuilder = new DbObjectBuilder(
            _context,
            _mapper
            );
        _service = new SailScores.Core.Services.SeriesService(
            _mockScoringCalculatorFactory.Object,
            _mockScoringService.Object,
            _mockHandicapService.Object,
            _mockForwarderService.Object,
            _mockConversionService.Object,
            _dbObjectBuilder,
            _context,
            _realCache,
            _mapper,
            _mockIndexNowService.Object
            );

    }

    [Fact]
    public async Task SaveSeries_Unlocked_CalculatesScores()
    {
        var seriesToUpdate = _mapper.Map<Series>(_fakeSeries);
        seriesToUpdate.Competitors = new List<Competitor> { };
        seriesToUpdate.ResultsLocked = false;
        await _service.Update(seriesToUpdate);

        _mockScoringCalculatorFactory.Verify(cf =>
            cf.CreateScoringCalculatorAsync(It.IsAny<ScoringSystem>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveSeries_Locked_DoesNotCalculateScores()
    {
        var seriesToUpdate = _mapper.Map<Series>(_fakeSeries);
        seriesToUpdate.ResultsLocked = true;
        await _service.Update(seriesToUpdate);

        _mockScoringCalculatorFactory.Verify(cf =>
            cf.CreateScoringCalculatorAsync(It.IsAny<ScoringSystem>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_HideDncDiscardsSetOnSeries_KeepsIt()
    {
        // Results views now hide discarded DNCs; the series flag is kept for the previous release.
        _context.Series.Single(s => s.Id == _fakeSeries.Id).HideDncDiscards = true;
        _context.SaveChanges();
        var seriesToUpdate = _mapper.Map<Series>(_fakeSeries);
        seriesToUpdate.ResultsLocked = true;
        seriesToUpdate.HideDncDiscards = false;

        await _service.Update(seriesToUpdate);

        Assert.True(_context.Series.Single(s => s.Id == _fakeSeries.Id).HideDncDiscards);
    }

    [Fact]
    public async Task GetSeriesDetailAsync_ReturnsFromDb()
    {
        // Arrange
        var season = await _context.Seasons.FirstAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _service.GetSeriesDetailsAsync(
            _clubInitials,
            season.UrlName,
            _seriesUrlName);


        // Assert
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(true, "Summary")]
    [InlineData(false, "Standard")]
    public async Task GetOneSeriesAsync_SummaryWithoutTemplate_UsesSummaryDefaultOrElseStandard(
        bool hasSummaryDefault, string expectedTemplate)
    {
        var club = _context.Clubs.First();
        var standard = new Database.Entities.SeriesResultsTemplate
        {
            Id = Guid.NewGuid(), ClubId = club.Id, Name = "Standard"
        };
        var summaryTemplate = new Database.Entities.SeriesResultsTemplate
        {
            Id = Guid.NewGuid(), ClubId = club.Id, Name = "Summary"
        };
        _context.SeriesResultsTemplates.AddRange(standard, summaryTemplate);
        club.DefaultSeriesResultsTemplateId = standard.Id;
        club.DefaultSummarySeriesResultsTemplateId = hasSummaryDefault ? summaryTemplate.Id : null;
        var series = new Database.Entities.Series
        {
            Id = Guid.NewGuid(), ClubId = club.Id, Name = "Season Summary", UrlName = "season-summary",
            Season = _context.Seasons.First(), Type = Database.Entities.SeriesType.Summary,
            RaceSeries = new List<Database.Entities.SeriesRace>()
        };
        _context.Series.Add(series);
        _context.HistoricalResults.Add(new Database.Entities.HistoricalResults
        {
            Id = Guid.NewGuid(), SeriesId = series.Id, IsCurrent = true, Created = DateTime.UtcNow,
            Results = Newtonsoft.Json.JsonConvert.SerializeObject(new FlatResults { Races = new List<FlatRace>() })
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetOneSeriesAsync(series.Id);

        Assert.Equal(expectedTemplate, result.SeriesResultsTemplate.Name);
    }

    [Fact]
    public async Task UpdateSeriesResultsAsync_SavesHistoricalResults()
    {
        var historicalResultCount = _context.HistoricalResults.Count();

        await _service.UpdateSeriesResults(_fakeSeries.Id, String.Empty);

        Assert.True(_context.HistoricalResults.Count() > historicalResultCount);
    }

    [Fact]
    public async Task GetAllSeriesAsync_WithDateRestriction_FiltersCorrectly()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();
        var testDate = season.Start.AddDays(5);

        // Create a date-restricted series
        var restrictedSeries = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Date Restricted Series",
            UrlName = "date-restricted-series",
            Season = season,
            Type = Database.Entities.SeriesType.Standard,
            DateRestricted = true,
            EnforcedStartDate = DateOnly.FromDateTime(testDate.AddDays(-2)),
            EnforcedEndDate = DateOnly.FromDateTime(testDate.AddDays(2))
        };

        // Create a non-restricted series
        var nonRestrictedSeries = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Non Restricted Series",
            UrlName = "non-restricted-series",
            Season = season,
            Type = Database.Entities.SeriesType.Standard,
            DateRestricted = false
        };

        _context.Series.Add(restrictedSeries);
        _context.Series.Add(nonRestrictedSeries);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - query with date within restricted series range
        var seriesWithinRange = await _service.GetAllSeriesAsync(clubId, testDate, false, false);

        // Assert - both series should be returned (restricted is within range, non-restricted has no restriction)
        Assert.Contains(seriesWithinRange, s => s.Id == restrictedSeries.Id);
        Assert.Contains(seriesWithinRange, s => s.Id == nonRestrictedSeries.Id);

        // Act - query with date outside restricted series range
        var dateOutsideRange = testDate.AddDays(10);
        var seriesOutsideRange = await _service.GetAllSeriesAsync(clubId, dateOutsideRange, false, false);

        // Assert - only non-restricted series should be returned
        Assert.DoesNotContain(seriesOutsideRange, s => s.Id == restrictedSeries.Id);
        Assert.Contains(seriesOutsideRange, s => s.Id == nonRestrictedSeries.Id);
    }

    [Fact]
    public async Task GetAllSeriesAsync_WithNullDate_ReturnsAllSeries()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();

        // Create a date-restricted series
        var restrictedSeries = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Restricted for Null Date Test",
            UrlName = "restricted-null-test",
            Season = season,
            Type = Database.Entities.SeriesType.Standard,
            DateRestricted = true,
            EnforcedStartDate = DateOnly.FromDateTime(season.Start),
            EnforcedEndDate = DateOnly.FromDateTime(season.End)
        };

        _context.Series.Add(restrictedSeries);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - query with null date
        var allSeries = await _service.GetAllSeriesAsync(clubId, null, false, false);

        // Assert - restricted series should be included when date is null
        Assert.Contains(allSeries, s => s.Id == restrictedSeries.Id);
    }

    [Fact]
    public async Task FlattenResults_WithDateRangedCustomFieldValues_ResolvesMultipleOrSingleValue()
    {
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();
        var competitor = new Competitor
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Comp With Dates",
            BoatName = "Boat",
            CustomFieldValues = new List<CompetitorFieldValue>()
        };

        var definition = new Database.Entities.CompetitorFieldDefinition
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Age",
            DisplayHeader = "Age",
            DataType = Database.Entities.CustomFieldDataType.Text,
            DisplayOrder = 1,
            IsActive = true
        };

        var firstValue = new CompetitorFieldValue
        {
            Id = Guid.NewGuid(),
            CompetitorId = competitor.Id,
            FieldDefinitionId = definition.Id,
            Value = "Junior",
            EffectiveFrom = new DateTime(2024, 1, 1),
            EffectiveTo = new DateTime(2024, 1, 10)
        };
        var secondValue = new CompetitorFieldValue
        {
            Id = Guid.NewGuid(),
            CompetitorId = competitor.Id,
            FieldDefinitionId = definition.Id,
            Value = "Senior",
            EffectiveFrom = new DateTime(2024, 1, 11),
            EffectiveTo = new DateTime(2024, 1, 20)
        };

        competitor.CustomFieldValues.Add(firstValue);
        competitor.CustomFieldValues.Add(secondValue);

        var raceOne = new Race
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Date = new DateTime(2024, 1, 5),
            Name = "Race 1",
            Scores = new List<Score>
            {
                new Score
                {
                    CompetitorId = competitor.Id,
                    Place = 1
                }
            }
        };
        var raceTwo = new Race
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Date = new DateTime(2024, 1, 15),
            Name = "Race 2",
            Scores = new List<Score>
            {
                new Score
                {
                    CompetitorId = competitor.Id,
                    Place = 2
                }
            }
        };

        _context.CompetitorFieldDefinitions.Add(definition);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mappedSeries = new SailScores.Core.Model.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Custom Field Series",
            UrlName = "custom-field-series",
            Season = _mapper.Map<Season>(season),
            Type = SailScores.Core.Model.SeriesType.Standard,
            Competitors = new List<Competitor> { competitor },
            Races = new List<Race> { raceOne, raceTwo },
            Results = new SeriesResults
            {
                Results = new Dictionary<Competitor, SeriesCompetitorResults>()
            }
        };

        var method = typeof(SeriesService).GetMethod("FlattenResults", BindingFlags.Instance | BindingFlags.NonPublic);
        var flatResults = (FlatResults)method.Invoke(_service, new object[] { mappedSeries });

        var flatCompetitor = Assert.Single(flatResults.Competitors);
        Assert.Equal("-multiple-", flatCompetitor.CustomFieldValues[definition.Id]);
    }

    [Fact]
    public async Task SaveNewSeries_SummaryWithChildren_PersistsChildLinks()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();

        var child1 = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Child Series 1",
            UrlName = "child-series-1",
            Season = season,
            Type = Database.Entities.SeriesType.Standard,
            RaceSeries = new List<Database.Entities.SeriesRace>(),
            ChildLinks = new List<Database.Entities.SeriesToSeriesLink>(),
            ParentLinks = new List<Database.Entities.SeriesToSeriesLink>()
        };
        var child2 = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Child Series 2",
            UrlName = "child-series-2",
            Season = season,
            Type = Database.Entities.SeriesType.Standard,
            RaceSeries = new List<Database.Entities.SeriesRace>(),
            ChildLinks = new List<Database.Entities.SeriesToSeriesLink>(),
            ParentLinks = new List<Database.Entities.SeriesToSeriesLink>()
        };

        _context.Series.Add(child1);
        _context.Series.Add(child2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var summary = new Series
        {
            ClubId = clubId,
            Name = "My Summary",
            Season = _mapper.Map<Season>(season),
            Type = SeriesType.Summary,
            ChildrenSeriesIds = new List<Guid> { child1.Id, child2.Id },
            UpdatedBy = "test"
        };

        // Act
        var summaryId = await _service.SaveNewSeries(summary);

        // Assert
        var dbSummary = await _context.Series
            .Include(s => s.ChildLinks)
            .SingleAsync(s => s.Id == summaryId, TestContext.Current.CancellationToken);

        Assert.NotNull(dbSummary.ChildLinks);
        Assert.Contains(dbSummary.ChildLinks, l => l.ChildSeriesId == child1.Id);
        Assert.Contains(dbSummary.ChildLinks, l => l.ChildSeriesId == child2.Id);
    }

    [Fact]
    public async Task UpdateSeriesResults_SummaryKeepingChildDiscards_LoadsChildDiscardsOntoRaces()
    {
        // Arrange: a summary of two child series, each with one race; the first child
        // discarded the competitor's score.
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();
        var fleet = _context.Fleets.First();
        var competitor = _context.Competitors.First();

        Database.Entities.Series MakeChild(string name)
        {
            var race = new Database.Entities.Race
            {
                Id = Guid.NewGuid(), ClubId = clubId, Name = $"{name} race", Fleet = fleet,
                Date = new DateTime(2026, 6, 6),
                Scores = new List<Database.Entities.Score>()
            };
            race.Scores.Add(new Database.Entities.Score
            {
                Id = Guid.NewGuid(), CompetitorId = competitor.Id, RaceId = race.Id, Place = 1
            });
            var child = new Database.Entities.Series
            {
                Id = Guid.NewGuid(), ClubId = clubId, Name = name, UrlName = name, Season = season,
                Type = Database.Entities.SeriesType.Standard,
                RaceSeries = new List<Database.Entities.SeriesRace>(),
                ChildLinks = new List<Database.Entities.SeriesToSeriesLink>(),
                ParentLinks = new List<Database.Entities.SeriesToSeriesLink>()
            };
            child.RaceSeries.Add(new Database.Entities.SeriesRace { Race = race, Series = child });
            return child;
        }

        var child1 = MakeChild("child-1");
        var child2 = MakeChild("child-2");
        var child1RaceId = child1.RaceSeries.Single().Race.Id;
        var summary = new Database.Entities.Series
        {
            Id = Guid.NewGuid(), ClubId = clubId, Name = "Summary", UrlName = "summary", Season = season,
            Type = Database.Entities.SeriesType.Summary,
            RaceSeries = new List<Database.Entities.SeriesRace>(),
            ChildLinks = new List<Database.Entities.SeriesToSeriesLink>(),
            ParentLinks = new List<Database.Entities.SeriesToSeriesLink>()
        };
        _context.Series.AddRange(child1, child2, summary);
        _context.SeriesToSeriesLinks.AddRange(
            new Database.Entities.SeriesToSeriesLink { ParentSeriesId = summary.Id, ChildSeriesId = child1.Id },
            new Database.Entities.SeriesToSeriesLink { ParentSeriesId = summary.Id, ChildSeriesId = child2.Id });
        var child1Results = new FlatResults
        {
            CalculatedScores = new[]
            {
                new FlatSeriesScore
                {
                    CompetitorId = competitor.Id,
                    Scores = new[] { new FlatCalculatedScore { RaceId = child1RaceId, Discard = true } }
                }
            }
        };
        _context.HistoricalResults.Add(new Database.Entities.HistoricalResults
        {
            Id = Guid.NewGuid(), SeriesId = child1.Id, IsCurrent = true, Created = DateTime.UtcNow,
            Results = Newtonsoft.Json.JsonConvert.SerializeObject(child1Results)
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockScoringService
            .Setup(s => s.GetScoringSystemFromCacheAsync(It.IsAny<Series>()))
            .ReturnsAsync(new ScoringSystem
            {
                Id = Guid.NewGuid(),
                ScoreCodes = new List<ScoreCode>(),
                EffectiveDiscardMethod = Api.Enumerations.DiscardMethod.PreserveChildSeriesDiscards
            });
        Series scoredSeries = null;
        _mockCalculator
            .Setup(c => c.CalculateResults(It.Is<Series>(s => s.Id == summary.Id)))
            .Callback<Series>(s => scoredSeries = s)
            .Returns(new SeriesResults { Results = new Dictionary<Competitor, SeriesCompetitorResults>() });

        // Act
        await _service.UpdateSeriesResults(summary.Id, "test", calculateParents: false);

        // Assert
        Assert.NotNull(scoredSeries);
        var race1 = scoredSeries.Races.Single(r => r.Id == child1RaceId);
        var race2 = scoredSeries.Races.Single(r => r.Id != child1RaceId);
        Assert.Equal(child1.Id, race1.SourceChildSeriesId);
        Assert.Equal(child2.Id, race2.SourceChildSeriesId);
        Assert.Contains(competitor.Id, race1.CompetitorIdsDiscardedInChild);
        Assert.Empty(race2.CompetitorIdsDiscardedInChild);

        // The saved results record each column's child series, for column separators.
        var saved = Newtonsoft.Json.JsonConvert.DeserializeObject<FlatResults>(
            _context.HistoricalResults.Single(r => r.SeriesId == summary.Id && r.IsCurrent).Results);
        Assert.Equal(child1.Id, saved.Races.Single(r => r.Id == child1RaceId).ChildSeriesId);
        Assert.Equal(child2.Id, saved.Races.Single(r => r.Id != child1RaceId).ChildSeriesId);
    }

    [Fact]
    public async Task UpdateSeriesResults_SummaryUsingChildScores_AddsMissedEveryRaceScoreForAbsentCompetitors()
    {
        // Arrange: a summary of two child series; each child has a different competitor.
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();
        var fleet = _context.Fleets.First();
        var competitorA = _context.Competitors.First();
        var competitorB = new Database.Entities.Competitor
        {
            Id = Guid.NewGuid(), ClubId = clubId, Name = "Second", SailNumber = "2", IsActive = true
        };
        _context.Competitors.Add(competitorB);

        Database.Entities.Series MakeChild(string name, Guid competitorId)
        {
            var race = new Database.Entities.Race
            {
                Id = Guid.NewGuid(), ClubId = clubId, Name = $"{name} race", Fleet = fleet,
                Date = new DateTime(2026, 6, 6),
                Scores = new List<Database.Entities.Score>()
            };
            race.Scores.Add(new Database.Entities.Score
            {
                Id = Guid.NewGuid(), CompetitorId = competitorId, RaceId = race.Id, Place = 1
            });
            var child = new Database.Entities.Series
            {
                Id = Guid.NewGuid(), ClubId = clubId, Name = name, UrlName = name, Season = season,
                Type = Database.Entities.SeriesType.Standard,
                RaceSeries = new List<Database.Entities.SeriesRace>(),
                ChildLinks = new List<Database.Entities.SeriesToSeriesLink>(),
                ParentLinks = new List<Database.Entities.SeriesToSeriesLink>()
            };
            child.RaceSeries.Add(new Database.Entities.SeriesRace { Race = race, Series = child });
            _context.HistoricalResults.Add(new Database.Entities.HistoricalResults
            {
                Id = Guid.NewGuid(), SeriesId = child.Id, IsCurrent = true, Created = DateTime.UtcNow,
                Results = Newtonsoft.Json.JsonConvert.SerializeObject(new FlatResults
                {
                    CalculatedScores = new[]
                    {
                        new FlatSeriesScore { CompetitorId = competitorId, Rank = 1, TotalScore = 1m }
                    }
                })
            });
            return child;
        }

        var child1 = MakeChild("child-1", competitorA.Id);
        var child2 = MakeChild("child-2", competitorB.Id);
        var summary = new Database.Entities.Series
        {
            Id = Guid.NewGuid(), ClubId = clubId, Name = "Summary", UrlName = "summary", Season = season,
            Type = Database.Entities.SeriesType.Summary,
            SummaryRollupMode = Api.Enumerations.SummaryRollupMode.ChildScoreAsRace,
            RaceSeries = new List<Database.Entities.SeriesRace>(),
            ChildLinks = new List<Database.Entities.SeriesToSeriesLink>(),
            ParentLinks = new List<Database.Entities.SeriesToSeriesLink>()
        };
        _context.Series.AddRange(child1, child2, summary);
        _context.SeriesToSeriesLinks.AddRange(
            new Database.Entities.SeriesToSeriesLink { ParentSeriesId = summary.Id, ChildSeriesId = child1.Id },
            new Database.Entities.SeriesToSeriesLink { ParentSeriesId = summary.Id, ChildSeriesId = child2.Id });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var system = new ScoringSystem { Id = Guid.NewGuid(), ScoreCodes = new List<ScoreCode>() };
        _mockScoringService
            .Setup(s => s.GetScoringSystemFromCacheAsync(It.IsAny<Series>()))
            .ReturnsAsync(system);
        _mockScoringService
            .Setup(s => s.GetScoringSystemsFromCacheAsync(It.IsAny<IList<Series>>()))
            .ReturnsAsync((IList<Series> list) => list.ToDictionary(s => s.Id, _ => system));
        _mockCalculator
            .Setup(c => c.CalculateAbsentCompetitorScore(It.IsAny<Series>()))
            .Returns(5m);
        Series scoredSeries = null;
        _mockCalculator
            .Setup(c => c.CalculateResults(It.Is<Series>(s => s.Id == summary.Id)))
            .Callback<Series>(s => scoredSeries = s)
            .Returns(new SeriesResults { Results = new Dictionary<Competitor, SeriesCompetitorResults>() });

        // Act
        await _service.UpdateSeriesResults(summary.Id, "test", calculateParents: false);

        // Assert
        Assert.NotNull(scoredSeries);
        var race1 = scoredSeries.Races.Single(r => r.Id == child1.Id);
        var race2 = scoredSeries.Races.Single(r => r.Id == child2.Id);
        var absentInRace1 = race1.Scores.Single(s => s.CompetitorId == competitorB.Id);
        var absentInRace2 = race2.Scores.Single(s => s.CompetitorId == competitorA.Id);
        Assert.Equal("DNC", absentInRace1.Code);
        Assert.Equal(5m, absentInRace1.PresetScoreValue);
        Assert.Equal(5m, absentInRace2.PresetScoreValue);
        Assert.Equal(1m, race1.Scores.Single(s => s.CompetitorId == competitorA.Id).PresetScoreValue);
        _mockScoringService.Verify(
            s => s.GetScoringSystemsFromCacheAsync(It.Is<IList<Series>>(l => l.Count == 2)),
            Times.Once);
    }


}
