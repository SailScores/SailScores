using AutoMapper;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Collections.Generic;

namespace SailScores.Test.Unit.Core.Services
{
    public class ScoringServiceTests
    {
        ScoringService _service;

        private readonly ISailScoresContext _context;
        private readonly Mock<IMemoryCache> _cache;
        private readonly IMapper _mapper;

        public ScoringServiceTests()
        {
            _context = InMemoryContextBuilder.GetContext();
            _cache = new Mock<IMemoryCache>();
            var cacheEntry = Mock.Of<ICacheEntry>();

            _cache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);

            _mapper = MapperBuilder.GetSailScoresMapper();
            _service = new ScoringService(
                _context,
                _cache.Object,
                _mapper);
        }

        [Fact]
        public async Task GetScoreCodesAsync_ReturnsOneDnc()
        {
            var scoringSystem = await _context.ScoringSystems.FirstAsync(ss => ss.ClubId != null, TestContext.Current.CancellationToken);
            _context.ScoreCodes.Add(new Database.Entities.ScoreCode
            {
                ScoringSystemId = scoringSystem.Id,
                Name = "DNC",
                CameToStart = false
            });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Act
            var results = await _service.GetScoreCodesAsync(scoringSystem.ClubId.Value);

            // Assert
            Assert.Equal(1, results.Count(sc => sc.Name == "DNC"));

        }

        [Fact]
        public async Task GetScoreCodesAsync_ParentSystemCode_IsIncluded()
        {
            var (root, child) = await AddRootAndChildSystemsAsync();
            root.ScoreCodes.Add(new Database.Entities.ScoreCode { Name = "PARENT" });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var results = await _service.GetScoreCodesAsync(child.ClubId.Value);

            Assert.Contains(results, sc => sc.Name == "PARENT");
        }

        [Fact]
        public async Task GetScoreCodesAsync_UnrelatedSharedSystemCode_IsExcluded()
        {
            var club = await _context.Clubs.FirstAsync(TestContext.Current.CancellationToken);
            _context.ScoringSystems.Add(new Database.Entities.ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Unrelated shared",
                ScoreCodes = new List<Database.Entities.ScoreCode>
                {
                    new Database.Entities.ScoreCode { Name = "UNRELATED" }
                }
            });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var results = await _service.GetScoreCodesAsync(club.Id);

            Assert.DoesNotContain(results, sc => sc.Name == "UNRELATED");
        }

        [Fact]
        public async Task GetScoreCodesAsync_SharedSystemUsedBySeries_IsIncluded()
        {
            var shared = new Database.Entities.ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Shared used by series",
                ScoreCodes = new List<Database.Entities.ScoreCode>
                {
                    new Database.Entities.ScoreCode { Name = "SCP" }
                }
            };
            _context.ScoringSystems.Add(shared);
            var series = await _context.Series.FirstAsync(TestContext.Current.CancellationToken);
            series.ScoringSystemId = shared.Id;
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var results = await _service.GetScoreCodesAsync(series.ClubId);

            Assert.Contains(results, sc => sc.Name == "SCP");
        }

        [Fact]
        public async Task GetScoreCodesAsync_ClubOverridesParentCode_ReturnsClubVersion()
        {
            var (root, child) = await AddRootAndChildSystemsAsync();
            root.ScoreCodes.Add(new Database.Entities.ScoreCode { Name = "ZFP", FormulaValue = 20 });
            child.ScoreCodes.Add(new Database.Entities.ScoreCode { Name = "ZFP", FormulaValue = 10 });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var results = await _service.GetScoreCodesAsync(child.ClubId.Value);

            Assert.Equal(10, Assert.Single(results, sc => sc.Name == "ZFP").FormulaValue);
        }

        [Fact]
        public async Task GetScoringSystemsAsync_ReturnsASystemWithDnc()
        {
            var club = await _context.Clubs.FirstAsync(TestContext.Current.CancellationToken);

            // Act
            var results = await _service.GetScoringSystemsAsync(club.Id, false);

            // Assert
            Assert.Contains(results, ss =>
                ss.ScoreCodes.Any(sc => sc.Name == "DNC") && ss.ClubId == club.Id);

        }

        [Fact]
        public async Task GetSiteDefaultSystemAsync_ReturnsOneSystem()
        {
            var club = await _context.Clubs.FirstAsync(TestContext.Current.CancellationToken);

            // Act
            var result = await _service.GetSiteDefaultSystemAsync();

            // Assert
            Assert.NotNull(result);

        }

        [Fact]
        public async Task GetScoringSystemAsync_ReturnsNotNull()
        {
            var scoringSystem = await _context.ScoringSystems.FirstAsync(ss => ss.ClubId != null, TestContext.Current.CancellationToken);
            _context.ScoreCodes.Add(new Database.Entities.ScoreCode
            {
                ScoringSystemId = scoringSystem.Id,
                Name = "DNC",
                CameToStart = false
            });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Act
            var result = await _service.GetScoringSystemAsync(scoringSystem.Id);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetScoringSystemAsync_WithGroupMappings_ReturnsMappedScoreCodeGroups()
        {
            var scoringSystem = await _context.ScoringSystems.FirstAsync(ss => ss.ClubId != null, TestContext.Current.CancellationToken);
            var dbGroup = new Database.Entities.ScoreCodeGroup
            {
                Id = Guid.NewGuid(),
                ScoringSystemId = scoringSystem.Id,
                Name = "Top two finishes",
                LimitationType = (int)ScoreCodeGroupLimitationType.NumberOfRaces,
                LimitationValue = 2M,
                UseNonDiscardedRaces = true,
                OverageCodeName = "DNC",
                OverageSelectionMethod = (int)ScoreCodeGroupOverageSelection.LatestFirst,
                Codes = new List<Database.Entities.ScoreCodeGroupCode>
                {
                    new() { CodeName = "DNC" },
                    new() { CodeName = "DNF" }
                }
            };

            _context.ScoreCodeGroups.Add(dbGroup);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _service.GetScoringSystemAsync(scoringSystem.Id);

            Assert.Contains(result.ScoreCodeGroups, g =>
                g.Name == "Top two finishes" &&
                g.IncludedCodeNames.Contains("DNC") &&
                g.LimitationType == ScoreCodeGroupLimitationType.NumberOfRaces);
        }

        [Fact]
        public async Task SaveScoringSystemAsync_NoId_SavesToDb()
        {
            var scoringSystem = new ScoringSystem
            {
                Name = "Hah"
            };


            // Act
            await _service.SaveScoringSystemAsync(scoringSystem);

            // Assert
            Assert.Contains(_context.ScoringSystems, (ss) => ss.Name == "Hah");
        }

        [Fact]
        public async Task DeleteScoringSystemAsync_NewSystem_RemovesFromDb()
        {
            var scoringSystem = new ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Hah"
            };

            await _service.SaveScoringSystemAsync(scoringSystem);
            Assert.Contains(_context.ScoringSystems, (ss) => ss.Name == "Hah");

            // Act again
            await _service.DeleteScoringSystemAsync(scoringSystem.Id);

            // Assert
            Assert.DoesNotContain(_context.ScoringSystems, (ss) => ss.Name == "Hah");
        }




        [Fact]
        public async Task GetScoreCodeAsync_ReturnsFromDb()
        {
            // Arrange
            var id = (await _context.ScoreCodes.FirstAsync(TestContext.Current.CancellationToken)).Id;
            // Act
            var result = _service.GetScoreCodeAsync(id);
            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SaveScoreCodeAsync_NewCode_SavesToDb()
        {
            // Arrange
            var system = await _context.ScoringSystems.FirstAsync(TestContext.Current.CancellationToken);
            var code = new ScoreCode
            {
                Name = "FIFO",
                ScoringSystemId = system.Id
            };
            // Act
            await _service.SaveScoreCodeAsync(code);

            // Assert
            Assert.Contains(_context.ScoreCodes, sc => sc.Name == "FIFO");
        }

        [Fact]
        public async Task SaveScoreCodeAsync_ExistingCode_SavesToDb()
        {
            // Arrange
            var existingCode = await _context.ScoreCodes.FirstAsync(TestContext.Current.CancellationToken);
            existingCode.Name = "HMM";

            var code = _mapper.Map<ScoreCode>(existingCode);
            
            // Act
            await _service.SaveScoreCodeAsync(code);

            // Assert
            Assert.Contains(_context.ScoreCodes, sc => sc.Name == "HMM");
        }

        [Fact]
        public async Task DeleteScoreCodeAsync_RemovesFromDb()
        {
            // Arrange
            var existingCode = await _context.ScoreCodes.FirstAsync(TestContext.Current.CancellationToken);
            Assert.Contains(_context.ScoreCodes, sc => sc.Id == existingCode.Id);

            // Act
            await _service.DeleteScoreCodeAsync(existingCode.Id);

            // Assert
            Assert.DoesNotContain(_context.ScoreCodes, sc => sc.Id == existingCode.Id);
        }

        [Fact]
        public async Task IsScoringSystemInUseAsync_NotInUse_ReturnsFalse()
        {

           // Act
           var result = await _service.IsScoringSystemInUseAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsScoringSystemInUseAsync_InUse_ReturnsTrue()
        {
            // Arrange
            var existingClub = await _context.Clubs.FirstAsync(TestContext.Current.CancellationToken);

            // Act
            var result = await _service.IsScoringSystemInUseAsync(existingClub.DefaultScoringSystemId.Value);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetScoringSystemAsync_ChildSystem_ResolvesCalculatorTypeFromRoot()
        {
            var (_, child) = await AddRootAndChildSystemsAsync();

            var result = await _service.GetScoringSystemAsync(child.Id);

            Assert.Equal(Api.Enumerations.ScoringCalculatorType.LowPointAverageExclDnc, result.CalculatorType);
        }

        // The Edit action loads every club and base system (tracked, with score codes)
        // before saving, so the saved system's ParentSystem and ScoreCodes are populated.
        [Fact]
        public async Task SaveScoringSystemAsync_AfterLoadingAllSystems_KeepsParentAndScoreCodes()
        {
            var club = await _context.Clubs.FirstAsync(TestContext.Current.CancellationToken);
            var baseSystem = await _context.ScoringSystems
                .FirstAsync(s => s.ClubId == null, TestContext.Current.CancellationToken);
            var child = new Database.Entities.ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Child",
                ClubId = club.Id,
                ParentSystemId = baseSystem.Id,
                DiscardPattern = "0",
                ScoreCodes = new List<Database.Entities.ScoreCode>
                {
                    new Database.Entities.ScoreCode { Name = "DNC", CameToStart = false }
                }
            };
            _context.ScoringSystems.Add(child);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            await _service.GetScoringSystemsAsync(club.Id, true);

            // Act
            await _service.SaveScoringSystemAsync(new ScoringSystem
            {
                Id = child.Id,
                Name = "Child renamed",
                ClubId = club.Id,
                ParentSystemId = baseSystem.Id,
                DiscardPattern = "0,1"
            });

            // Assert
            var saved = await _context.ScoringSystems.AsNoTracking()
                .Include(s => s.ScoreCodes)
                .SingleAsync(s => s.Id == child.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Child renamed", saved.Name);
            Assert.Equal(baseSystem.Id, saved.ParentSystemId);
            Assert.Single(saved.ScoreCodes);
        }

        [Fact]
        public async Task SaveScoringSystemAsync_ChildWithResolvedType_DoesNotWriteCalculatorType()
        {
            var (_, child) = await AddRootAndChildSystemsAsync();
            var model = await _service.GetScoringSystemAsync(child.Id);
            model.Name = "Renamed child";

            await _service.SaveScoringSystemAsync(model);

            var dbChild = await _context.ScoringSystems.SingleAsync(s => s.Id == child.Id, TestContext.Current.CancellationToken);
            Assert.Equal("Renamed child", dbChild.Name);
            Assert.Null(dbChild.CalculatorType);
        }

        [Fact]
        public async Task SaveScoringSystemAsync_WithSettings_StoresJsonAndChildInheritsValues()
        {
            var (root, child) = await AddRootAndChildSystemsAsync();
            var rootModel = await _service.GetScoringSystemAsync(root.Id);
            rootModel.Settings = new ScoringSystemSettings { PenaltyThresholdPercent = 70m };
            rootModel.DiscardMethod = Api.Enumerations.DiscardMethod.PerDay;
            await _service.SaveScoringSystemAsync(rootModel);

            var childModel = await _service.GetScoringSystemAsync(child.Id);
            childModel.Settings = new ScoringSystemSettings { PenaltyScoreValue = 8m };
            await _service.SaveScoringSystemAsync(childModel);

            var result = await _service.GetScoringSystemAsync(child.Id);
            var dbChild = await _context.ScoringSystems.SingleAsync(s => s.Id == child.Id, TestContext.Current.CancellationToken);

            Assert.Equal("{\"PenaltyScoreValue\":8}", dbChild.Settings);
            Assert.Equal(8m, result.Settings.PenaltyScoreValue);
            Assert.Null(result.Settings.PenaltyThresholdPercent);
            Assert.Equal(70m, result.EffectiveSettings.PenaltyThresholdPercent);
            Assert.Null(result.DiscardMethod);
            Assert.Equal(Api.Enumerations.DiscardMethod.PerDay, result.EffectiveDiscardMethod);
        }

        [Fact]
        public async Task GetScoringSystemsAsync_ChildSystem_ResolvesInheritedValues()
        {
            var (_, child) = await AddRootAndChildSystemsAsync();

            var results = await _service.GetScoringSystemsAsync(child.ClubId.Value, true);

            var childResult = results.Single(s => s.Id == child.Id);
            Assert.Equal(Api.Enumerations.ScoringCalculatorType.LowPointAverageExclDnc, childResult.CalculatorType);
        }

        [Fact]
        public async Task GetScoringSystemsAsync_RestrictedBaseSystem_OnlyOfferedToAllowedClubs()
        {
            var (root, _) = await AddRootAndChildSystemsAsync();
            var allowedClub = await _context.Clubs.FirstAsync(TestContext.Current.CancellationToken);
            var otherClubId = await AddClubAsync("OTHER");
            await _service.SaveSharedScoringSystemAccessAsync(root.Id, true, new[] { allowedClub.Id });

            var allowed = await _service.GetScoringSystemsAsync(allowedClub.Id, true);
            var other = await _service.GetScoringSystemsAsync(otherClubId, true);

            Assert.Contains(allowed, s => s.Id == root.Id);
            Assert.DoesNotContain(other, s => s.Id == root.Id);
            Assert.Contains(other, s => s.ClubId == null);
        }

        [Fact]
        public async Task SaveSharedScoringSystemAccessAsync_Unrestricted_ClearsClubsAndOffersToAll()
        {
            var (root, _) = await AddRootAndChildSystemsAsync();
            var club = await _context.Clubs.FirstAsync(TestContext.Current.CancellationToken);
            var otherClubId = await AddClubAsync("OTHER");
            await _service.SaveSharedScoringSystemAccessAsync(root.Id, true, new[] { club.Id });

            await _service.SaveSharedScoringSystemAccessAsync(root.Id, false, new[] { club.Id });

            var access = (await _service.GetSharedScoringSystemAccessAsync()).Single(s => s.Id == root.Id);
            Assert.False(access.IsRestrictedToClubs);
            Assert.Empty(access.ClubIds);
            var other = await _service.GetScoringSystemsAsync(otherClubId, true);
            Assert.Contains(other, s => s.Id == root.Id);
        }

        [Fact]
        public async Task SaveSharedScoringSystemAccessAsync_ChangedClubs_ReplacesAccessList()
        {
            var (root, _) = await AddRootAndChildSystemsAsync();
            var club = await _context.Clubs.FirstAsync(TestContext.Current.CancellationToken);
            var otherClubId = await AddClubAsync("OTHER");
            await _service.SaveSharedScoringSystemAccessAsync(root.Id, true, new[] { club.Id });

            await _service.SaveSharedScoringSystemAccessAsync(root.Id, true, new[] { otherClubId, Guid.NewGuid() });

            var access = (await _service.GetSharedScoringSystemAccessAsync()).Single(s => s.Id == root.Id);
            Assert.True(access.IsRestrictedToClubs);
            Assert.Equal(new[] { otherClubId }, access.ClubIds);
            Assert.Equal(root.Name, access.Name);
        }

        [Fact]
        public async Task SaveSharedScoringSystemAccessAsync_SiteDefaultSystem_Throws()
        {
            var siteDefault = await _context.ScoringSystems
                .FirstAsync(s => s.IsSiteDefault == true, TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SaveSharedScoringSystemAccessAsync(siteDefault.Id, true, Array.Empty<Guid>()));
        }

        [Fact]
        public async Task SaveSharedScoringSystemAccessAsync_ClubSystem_Throws()
        {
            var (_, child) = await AddRootAndChildSystemsAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SaveSharedScoringSystemAccessAsync(child.Id, true, Array.Empty<Guid>()));
        }

        [Fact]
        public async Task GetSharedScoringSystemAccessAsync_SiteDefault_IsUsedForNewClubs()
        {
            var (root, child) = await AddRootAndChildSystemsAsync();

            var access = await _service.GetSharedScoringSystemAccessAsync();

            Assert.Contains(access, s => s.IsUsedForNewClubs);
            Assert.False(access.Single(s => s.Id == root.Id).IsUsedForNewClubs);
            Assert.DoesNotContain(access, s => s.Id == child.Id);
        }

        [Fact]
        public async Task GetScoringSystemAsync_ChildWithoutPatternOrParticipation_InheritsFromParent()
        {
            var (root, child) = await AddRootAndChildSystemsAsync();
            root.DiscardPattern = "0,0,0,1";
            root.ParticipationPercent = 50m;
            child.DiscardPattern = null;
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var system = await _service.GetScoringSystemAsync(child.Id, true);

            Assert.Null(system.DiscardPattern);
            Assert.Null(system.ParticipationPercent);
            Assert.Equal("0,0,0,1", system.EffectiveDiscardPattern);
            Assert.Equal(50m, system.EffectiveParticipationPercent);
        }

        private async Task<Guid> AddClubAsync(string initials)
        {
            var club = new Database.Entities.Club
            {
                Id = Guid.NewGuid(),
                Name = $"{initials} Club",
                Initials = initials
            };
            _context.Clubs.Add(club);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return club.Id;
        }

        private async Task<(Database.Entities.ScoringSystem root, Database.Entities.ScoringSystem child)>
            AddRootAndChildSystemsAsync()
        {
            var root = new Database.Entities.ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Base LPA",
                DiscardPattern = "0",
                CalculatorType = Api.Enumerations.ScoringCalculatorType.LowPointAverageExclDnc,
                ScoreCodes = new List<Database.Entities.ScoreCode>()
            };
            var child = new Database.Entities.ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Club LPA",
                DiscardPattern = "0",
                ClubId = (await _context.Clubs.FirstAsync(TestContext.Current.CancellationToken)).Id,
                ParentSystemId = root.Id,
                ScoreCodes = new List<Database.Entities.ScoreCode>()
            };
            _context.ScoringSystems.Add(root);
            _context.ScoringSystems.Add(child);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return (root, child);
        }

    }
}
