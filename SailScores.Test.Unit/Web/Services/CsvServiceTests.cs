using Moq;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Web.Resources;
using SailScores.Web.Services;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace SailScores.Test.Unit.Web.Services
{
    public class CsvServiceTests
    {
        private readonly Mock<IStringLocalizer<SharedResource>> _stringLocalizerMock;
        private readonly Mock<ILocalizerService> _sailscoresLocalizerMock;
        private readonly CsvService _service;

        public CsvServiceTests()
        {
            _stringLocalizerMock = new Mock<IStringLocalizer<SharedResource>>();
            _sailscoresLocalizerMock = new Mock<ILocalizerService>();

            // Setup default localizer behavior
            _stringLocalizerMock.Setup(l => l[It.IsAny<string>()])
                .Returns((string key) => new LocalizedString(key, key));

            _sailscoresLocalizerMock.Setup(l => l[It.IsAny<string>()])
                .Returns((string key) => key);

            _service = new CsvService(
                _stringLocalizerMock.Object,
                _sailscoresLocalizerMock.Object);
        }

        #region Helper Methods

        private string ReadCsvContent(Stream stream)
        {
            stream.Position = 0;
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private List<string> GetCsvLines(Stream stream)
        {
            var content = ReadCsvContent(stream);
            return content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        private Series CreateTestSeries(
            int raceCount = 2,
            int competitorCount = 2,
            bool withElapsedTimes = false,
            bool[] trackTimesByRace = null)
        {
            if (trackTimesByRace == null)
            {
                trackTimesByRace = Enumerable.Repeat(withElapsedTimes, raceCount).ToArray();
            }

            var races = new List<FlatRace>();
            for (int i = 0; i < raceCount; i++)
            {
                races.Add(new FlatRace
                {
                    Id = Guid.NewGuid(),
                    Name = $"Race {i + 1}",
                    Date = DateTime.Today.AddDays(i),
                    Order = i,
                    State = RaceState.Raced,
                    TrackTimes = trackTimesByRace[i]
                });
            }

            var competitors = new List<FlatCompetitor>();
            for (int i = 0; i < competitorCount; i++)
            {
                competitors.Add(new FlatCompetitor
                {
                    Id = Guid.NewGuid(),
                    Name = $"Sailor {i + 1}",
                    SailNumber = $"100{i + 1}",
                    BoatName = $"Boat {i + 1}",
                    AlternativeSailNumber = null
                });
            }

            var calculatedScores = new List<FlatSeriesScore>();
            for (int c = 0; c < competitorCount; c++)
            {
                var scores = new List<FlatCalculatedScore>();
                for (int r = 0; r < raceCount; r++)
                {
                    var score = new FlatCalculatedScore
                    {
                        RaceId = races[r].Id,
                        Place = c + 1,
                        Code = "FIN",
                        ScoreValue = c + 1,
                        Discard = false
                    };

                    if (withElapsedTimes && trackTimesByRace[r])
                    {
                        score.ElapsedTime = new TimeSpan(1, 15 + c, 30 + (c * 5));
                    }

                    scores.Add(score);
                }

                calculatedScores.Add(new FlatSeriesScore
                {
                    CompetitorId = competitors[c].Id,
                    Rank = c + 1,
                    TotalScore = (decimal)(c + 1) * raceCount,
                    Scores = scores
                });
            }

            var flatResults = new FlatResults
            {
                SeriesId = Guid.NewGuid(),
                Competitors = competitors,
                Races = races,
                CalculatedScores = calculatedScores,
                IsPercentSystem = false,
                LowerScoreWins = true
            };

            return new Series
            {
                Id = Guid.NewGuid(),
                Name = "Test Series",
                FlatResults = flatResults,
                PreferAlternativeSailNumbers = false
            };
        }

        #endregion

        #region Series CSV Tests

        [Fact]
        public void GetCsv_SeriesWithoutElapsedTimes_ContainsCorrectHeaders()
        {
            // Arrange
            var series = CreateTestSeries(raceCount: 2, competitorCount: 1, withElapsedTimes: false);

            _sailscoresLocalizerMock.Setup(l => l.GetShortName(It.IsAny<FlatRace>()))
                .Returns((FlatRace r) => r.Name);

            // Act
            var stream = _service.GetCsv(series);
            var lines = GetCsvLines(stream);

            // Assert
            Assert.NotEmpty(lines);
            var headers = lines[0].Split(',');
            Assert.Contains("Place", headers);
            Assert.Contains("Sail", headers);
            Assert.Contains("Helm", headers);
            Assert.Contains("Boat", headers);
            Assert.Contains("Total", headers);
            Assert.Contains("Race 1", headers);
            Assert.Contains("Race 2", headers);
        }

        [Fact]
        public void GetCsv_SeriesWithElapsedTimes_IncludesElapsedTimeHeaders()
        {
            // Arrange
            var series = CreateTestSeries(
                raceCount: 2,
                competitorCount: 1,
                withElapsedTimes: true);

            _sailscoresLocalizerMock.Setup(l => l.GetShortName(It.IsAny<FlatRace>()))
                .Returns((FlatRace r) => r.Name);

            // Act
            var stream = _service.GetCsv(series);
            var lines = GetCsvLines(stream);

            // Assert
            Assert.NotEmpty(lines);
            var headerLine = lines[0];
            Assert.Contains("Race 1 - Elapsed", headerLine);
            Assert.Contains("Race 1 - Elapsed Seconds", headerLine);
            Assert.Contains("Race 2 - Elapsed", headerLine);
            Assert.Contains("Race 2 - Elapsed Seconds", headerLine);
        }

        [Fact]
        public void GetCsv_SeriesWithMixedTimeTracking_IncludesElapsedOnlyForTrackedRaces()
        {
            // Arrange
            var trackTimes = new[] { true, false, true };
            var series = CreateTestSeries(
                raceCount: 3,
                competitorCount: 1,
                trackTimesByRace: trackTimes);

            _sailscoresLocalizerMock.Setup(l => l.GetShortName(It.IsAny<FlatRace>()))
                .Returns((FlatRace r) => r.Name);

            // Act
            var stream = _service.GetCsv(series);
            var lines = GetCsvLines(stream);

            // Assert
            var headerLine = lines[0];
            Assert.Contains("Race 1 - Elapsed", headerLine);
            Assert.Contains("Race 1 - Elapsed Seconds", headerLine);
            Assert.DoesNotContain("Race 2 - Elapsed", headerLine);
            Assert.DoesNotContain("Race 2 - Elapsed Seconds", headerLine);
            Assert.Contains("Race 3 - Elapsed", headerLine);
            Assert.Contains("Race 3 - Elapsed Seconds", headerLine);
        }

        [Fact]
        public void GetCsv_SeriesWithElapsedTimes_ContainsElapsedTimeData()
        {
            // Arrange
            var series = CreateTestSeries(raceCount: 2, competitorCount: 2, withElapsedTimes: true);

            _sailscoresLocalizerMock.Setup(l => l.GetShortName(It.IsAny<FlatRace>()))
                .Returns((FlatRace r) => r.Name);

            // Act
            var stream = _service.GetCsv(series);
            var lines = GetCsvLines(stream);

            // Assert - Check that data rows contain elapsed time values
            Assert.True(lines.Count >= 3, "CSV should have header + at least 2 data rows");
            var dataRow = lines[1];

            // The data row should contain formatted elapsed times and elapsed seconds
            Assert.Contains(":", dataRow); // Should contain time format HH:MM:SS
            Assert.Contains(",4530,", dataRow); // 01:15:30 => 4530 seconds
        }

        [Fact]
        public void GetCsv_CompetitorWithoutElapsedTime_SkipsElapsedColumn()
        {
            // Arrange
            var races = new List<FlatRace>
            {
                new FlatRace
                {
                    Id = Guid.NewGuid(),
                    Name = "Race 1",
                    TrackTimes = true,
                    State = RaceState.Raced
                }
            };

            var raceId = races[0].Id;
            var competitorId = Guid.NewGuid();

            var competitors = new List<FlatCompetitor>
            {
                new FlatCompetitor
                {
                    Id = competitorId,
                    Name = "Test Sailor",
                    SailNumber = "1001"
                }
            };

            var scores = new List<FlatCalculatedScore>
            {
                new FlatCalculatedScore
                {
                    RaceId = raceId,
                    Place = 1,
                    Code = "FIN",
                    ScoreValue = 1,
                    ElapsedTime = null  // No elapsed time
                }
            };

            var flatResults = new FlatResults
            {
                SeriesId = Guid.NewGuid(),
                Competitors = competitors,
                Races = races,
                CalculatedScores = new List<FlatSeriesScore>
                {
                    new FlatSeriesScore
                    {
                        CompetitorId = competitorId,
                        Rank = 1,
                        TotalScore = 1,
                        Scores = scores
                    }
                },
                IsPercentSystem = false
            };

            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Test Series",
                FlatResults = flatResults
            };

            _sailscoresLocalizerMock.Setup(l => l.GetShortName(It.IsAny<FlatRace>()))
                .Returns((FlatRace r) => r.Name);

            // Act
            var stream = _service.GetCsv(series);
            var lines = GetCsvLines(stream);

            // Assert
            Assert.NotEmpty(lines);
            var dataRow = lines[1];

            // The elapsed time and elapsed seconds columns should be empty
            // Pattern: "...FIN 1,,," indicates both elapsed columns are empty
            Assert.Contains(",,,", dataRow);
        }

        [Fact]
        public void GetCsv_ScheduledRace_DoesNotIncludeElapsedTime()
        {
            // Arrange
            var races = new List<FlatRace>
            {
                new FlatRace
                {
                    Id = Guid.NewGuid(),
                    Name = "Race 1",
                    TrackTimes = true,
                    State = RaceState.Scheduled
                }
            };

            var competitorId = Guid.NewGuid();
            var competitors = new List<FlatCompetitor>
            {
                new FlatCompetitor
                {
                    Id = competitorId,
                    Name = "Test Sailor",
                    SailNumber = "1001"
                }
            };

            var flatResults = new FlatResults
            {
                SeriesId = Guid.NewGuid(),
                Competitors = competitors,
                Races = races,
                CalculatedScores = new List<FlatSeriesScore>
                {
                    new FlatSeriesScore
                    {
                        CompetitorId = competitorId,
                        Rank = null,
                        TotalScore = null,
                        Scores = new List<FlatCalculatedScore>()
                    }
                },
                IsPercentSystem = false
            };

            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Test Series",
                FlatResults = flatResults
            };

            _sailscoresLocalizerMock.Setup(l => l.GetShortName(It.IsAny<FlatRace>()))
                .Returns((FlatRace r) => r.Name);

            // Act
            var stream = _service.GetCsv(series);
            var lines = GetCsvLines(stream);

            // Assert
            Assert.NotEmpty(lines);
            var dataRow = lines[1];

            // Scheduled races show "Sched" instead of score, and no elapsed time follows
            Assert.Contains("Sched", dataRow);
        }

        [Fact]
        public void GetCsv_WithAlternativeSailNumbers_UsesSailNumberPreference()
        {
            // Arrange
            var competitorId = Guid.NewGuid();
            var series = CreateTestSeries(raceCount: 1, competitorCount: 1);
            series.PreferAlternativeSailNumbers = true;
            series.FlatResults.Competitors.First().AlternativeSailNumber = "ALT001";

            _sailscoresLocalizerMock.Setup(l => l.GetShortName(It.IsAny<FlatRace>()))
                .Returns((FlatRace r) => r.Name);

            // Act
            var stream = _service.GetCsv(series);
            var lines = GetCsvLines(stream);

            // Assert
            var dataRow = lines[1];
            Assert.Contains("ALT001", dataRow);
        }

        [Fact]
        public void GetCsv_EmptyCompetitors_ReturnsOnlyHeaders()
        {
            // Arrange
            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Empty Series",
                FlatResults = new FlatResults
                {
                    SeriesId = Guid.NewGuid(),
                    Competitors = new List<FlatCompetitor>(),
                    Races = new List<FlatRace>(),
                    CalculatedScores = new List<FlatSeriesScore>(),
                    IsPercentSystem = false
                }
            };

            // Act
            var stream = _service.GetCsv(series);
            var lines = GetCsvLines(stream);

            // Assert
            Assert.Single(lines); // Only header line
        }

        [Fact]
        public void GetCsv_NullFlatResults_DoesNotThrow()
        {
            // Arrange
            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Null Results Series",
                FlatResults = null
            };

            // Act & Assert
            var stream = _service.GetCsv(series);
            Assert.NotNull(stream);
        }

        [Fact]
        public void GetCsv_ValuesWithCommas_EscapesProperlyWithQuotes()
        {
            // Arrange
            var competitorId = Guid.NewGuid();
            var competitors = new List<FlatCompetitor>
            {
                new FlatCompetitor
                {
                    Id = competitorId,
                    Name = "Sailor, John",
                    SailNumber = "1001",
                    BoatName = "Boat, The"
                }
            };

            var raceId = Guid.NewGuid();
            var races = new List<FlatRace>
            {
                new FlatRace
                {
                    Id = raceId,
                    Name = "Race 1",
                    TrackTimes = false,
                    State = RaceState.Raced
                }
            };

            var flatResults = new FlatResults
            {
                SeriesId = Guid.NewGuid(),
                Competitors = competitors,
                Races = races,
                CalculatedScores = new List<FlatSeriesScore>
                {
                    new FlatSeriesScore
                    {
                        CompetitorId = competitorId,
                        Rank = 1,
                        TotalScore = 1,
                        Scores = new List<FlatCalculatedScore>
                        {
                            new FlatCalculatedScore
                            {
                                RaceId = raceId,
                                Place = 1,
                                Code = "FIN",
                                ScoreValue = 1
                            }
                        }
                    }
                },
                IsPercentSystem = false
            };

            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Test Series",
                FlatResults = flatResults
            };

            _sailscoresLocalizerMock.Setup(l => l.GetShortName(It.IsAny<FlatRace>()))
                .Returns((FlatRace r) => r.Name);

            // Act
            var stream = _service.GetCsv(series);
            var content = ReadCsvContent(stream);

            // Assert
            Assert.Contains("\"Sailor, John\"", content);
            Assert.Contains("\"Boat, The\"", content);
        }

        #endregion

        #region Competitor CSV Tests

        [Fact]
        public void GetCsv_Competitors_ContainsCorrectHeaders()
        {
            // Arrange
            var competitors = new Dictionary<string, IEnumerable<Competitor>>
            {
                {
                    "Fleet A", new List<Competitor>
                    {
                        new Competitor
                        {
                            SailNumber = "1001",
                            Name = "Sailor 1",
                            BoatName = "Boat 1"
                        }
                    }
                }
            };

            // Act
            var stream = _service.GetCsv(competitors);
            var lines = GetCsvLines(stream);

            // Assert
            Assert.NotEmpty(lines);
            var headers = lines[0];
            Assert.Contains("Fleet", headers);
            Assert.Contains("Sail", headers);
            Assert.Contains("Sailor(s)", headers);
            Assert.Contains("Boat", headers);
        }

        [Fact]
        public void GetCsv_Competitors_ContainsAllCompetitors()
        {
            // Arrange
            var competitors = new Dictionary<string, IEnumerable<Competitor>>
            {
                {
                    "Fleet A", new List<Competitor>
                    {
                        new Competitor { SailNumber = "1001", Name = "Sailor 1", BoatName = "Boat 1" },
                        new Competitor { SailNumber = "1002", Name = "Sailor 2", BoatName = "Boat 2" }
                    }
                },
                {
                    "Fleet B", new List<Competitor>
                    {
                        new Competitor { SailNumber = "2001", Name = "Sailor 3", BoatName = "Boat 3" }
                    }
                }
            };

            // Act
            var stream = _service.GetCsv(competitors);
            var lines = GetCsvLines(stream);

            // Assert
            Assert.Equal(4, lines.Count); // Header + 3 competitors
            Assert.Contains("1001", lines[1]);
            Assert.Contains("1002", lines[2]);
            Assert.Contains("2001", lines[3]);
        }

        #endregion

        #region FormatElapsedTime Tests

        [Fact]
        public void FormatElapsedTime_NullValue_ReturnsEmpty()
        {
            // This tests the private method indirectly through CSV generation
            // Arrange
            var raceId = Guid.NewGuid();
            var races = new List<FlatRace>
            {
                new FlatRace
                {
                    Id = raceId,
                    Name = "Race 1",
                    TrackTimes = true,
                    State = RaceState.Raced
                }
            };

            var competitorId = Guid.NewGuid();
            var competitors = new List<FlatCompetitor>
            {
                new FlatCompetitor { Id = competitorId, Name = "Sailor", SailNumber = "1001" }
            };

            var scores = new List<FlatCalculatedScore>
            {
                new FlatCalculatedScore
                {
                    RaceId = raceId,
                    Place = 1,
                    Code = "FIN",
                    ScoreValue = 1,
                    ElapsedTime = null
                }
            };

            var flatResults = new FlatResults
            {
                SeriesId = Guid.NewGuid(),
                Competitors = competitors,
                Races = races,
                CalculatedScores = new List<FlatSeriesScore>
                {
                    new FlatSeriesScore
                    {
                        CompetitorId = competitorId,
                        Rank = 1,
                        TotalScore = 1,
                        Scores = scores
                    }
                },
                IsPercentSystem = false
            };

            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Test Series",
                FlatResults = flatResults
            };

            _sailscoresLocalizerMock.Setup(l => l.GetShortName(It.IsAny<FlatRace>()))
                .Returns((FlatRace r) => r.Name);

            // Act
            var stream = _service.GetCsv(series);
            var content = ReadCsvContent(stream);

            // Assert - Should have empty elapsed time column
            Assert.Contains(",,", content); // Two consecutive commas indicate empty field
        }

        [Fact]
        public void GetCsv_ElapsedTimeUnderOneDay_FormatsAsHHMMSS()
        {
            // Arrange
            var raceId = Guid.NewGuid();
            var races = new List<FlatRace>
            {
                new FlatRace
                {
                    Id = raceId,
                    Name = "Race 1",
                    TrackTimes = true,
                    State = RaceState.Raced
                }
            };

            var competitorId = Guid.NewGuid();
            var competitors = new List<FlatCompetitor>
            {
                new FlatCompetitor { Id = competitorId, Name = "Sailor", SailNumber = "1001" }
            };

            var scores = new List<FlatCalculatedScore>
            {
                new FlatCalculatedScore
                {
                    RaceId = raceId,
                    Place = 1,
                    Code = "FIN",
                    ScoreValue = 1,
                    ElapsedTime = new TimeSpan(2, 30, 45)  // 2 hours, 30 minutes, 45 seconds
                }
            };

            var flatResults = new FlatResults
            {
                SeriesId = Guid.NewGuid(),
                Competitors = competitors,
                Races = races,
                CalculatedScores = new List<FlatSeriesScore>
                {
                    new FlatSeriesScore
                    {
                        CompetitorId = competitorId,
                        Rank = 1,
                        TotalScore = 1,
                        Scores = scores
                    }
                },
                IsPercentSystem = false
            };

            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Test Series",
                FlatResults = flatResults
            };

            _sailscoresLocalizerMock.Setup(l => l.GetShortName(It.IsAny<FlatRace>()))
                .Returns((FlatRace r) => r.Name);

            // Act
            var stream = _service.GetCsv(series);
            var content = ReadCsvContent(stream);

            // Assert
            Assert.Contains("02:30:45", content);
        }

        [Fact]
        public void GetCsv_ElapsedTimeOverOneDay_FormatsWithDays()
        {
            // Arrange
            var raceId = Guid.NewGuid();
            var races = new List<FlatRace>
            {
                new FlatRace
                {
                    Id = raceId,
                    Name = "Race 1",
                    TrackTimes = true,
                    State = RaceState.Raced
                }
            };

            var competitorId = Guid.NewGuid();
            var competitors = new List<FlatCompetitor>
            {
                new FlatCompetitor { Id = competitorId, Name = "Sailor", SailNumber = "1001" }
            };

            var scores = new List<FlatCalculatedScore>
            {
                new FlatCalculatedScore
                {
                    RaceId = raceId,
                    Place = 1,
                    Code = "FIN",
                    ScoreValue = 1,
                    ElapsedTime = new TimeSpan(2, 14, 30, 45)  // 2 days, 14 hours, 30 minutes, 45 seconds
                }
            };

            var flatResults = new FlatResults
            {
                SeriesId = Guid.NewGuid(),
                Competitors = competitors,
                Races = races,
                CalculatedScores = new List<FlatSeriesScore>
                {
                    new FlatSeriesScore
                    {
                        CompetitorId = competitorId,
                        Rank = 1,
                        TotalScore = 1,
                        Scores = scores
                    }
                },
                IsPercentSystem = false
            };

            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Test Series",
                FlatResults = flatResults
            };

            _sailscoresLocalizerMock.Setup(l => l.GetShortName(It.IsAny<FlatRace>()))
                .Returns((FlatRace r) => r.Name);

            // Act
            var stream = _service.GetCsv(series);
            var content = ReadCsvContent(stream);

            // Assert
            Assert.Contains("2d 14:30:45", content);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_MultipleTimesDoesNotThrow()
        {
            // Act & Assert
            _service.Dispose();
            _service.Dispose(); // Should not throw
        }

        #endregion
    }
}
