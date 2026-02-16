using AutoMapper;
using Moq;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Web.Services
{
    public class ClubRequestServiceTests
    {
        private readonly Mock<SailScores.Core.Services.IClubService> _coreClubServiceMock;
        private readonly Mock<SailScores.Core.Services.IClubRequestService> _coreClubRequestServiceMock;
        private readonly Mock<SailScores.Core.Services.IScoringService> _coreScoringServiceMock;
        private readonly Mock<SailScores.Core.Services.IUserService> _coreUserServiceMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> _configurationMock;
        private readonly Mock<Microsoft.Extensions.Caching.Memory.IMemoryCache> _memoryCacheMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ClubRequestService _service;

        private readonly Guid _requestId = Guid.NewGuid();
        private readonly Guid _newClubId = Guid.NewGuid();
        private readonly Guid _copyFromClubId = Guid.NewGuid();

        public ClubRequestServiceTests()
        {
            _coreClubServiceMock = new Mock<SailScores.Core.Services.IClubService>();
            _coreClubRequestServiceMock = new Mock<SailScores.Core.Services.IClubRequestService>();
            _coreScoringServiceMock = new Mock<SailScores.Core.Services.IScoringService>();
            _coreUserServiceMock = new Mock<SailScores.Core.Services.IUserService>();
            _emailSenderMock = new Mock<IEmailSender>();
            _configurationMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            _memoryCacheMock = new Mock<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            _mapperMock = new Mock<IMapper>();

            _service = new ClubRequestService(
                _coreClubServiceMock.Object,
                _coreClubRequestServiceMock.Object,
                _coreScoringServiceMock.Object,
                _coreUserServiceMock.Object,
                _emailSenderMock.Object,
                _configurationMock.Object,
                _memoryCacheMock.Object,
                _mapperMock.Object);
        }

        [Fact]
        public async Task ProcessRequest_WithCopyFromClubId_CopiesClubAndSetsTestClubId()
        {
            // Arrange
            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "Test Club",
                ClubInitials = "tst",
                ContactEmail = "test@example.com",
                RequestApproved = null
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.CopyClubAsync(_copyFromClubId, It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessRequest(_requestId, test: true, copyFromClubId: _copyFromClubId);

            // Assert
            _coreClubServiceMock.Verify(
                s => s.CopyClubAsync(
                    _copyFromClubId,
                    It.Is<Club>(c => c.Name == "Test Club"
                        && c.Initials == "TSTTEST"
                        && c.IsHidden == true)),
                Times.Once);

            _coreClubRequestServiceMock.Verify(s => s.UpdateRequest(
                It.Is<ClubRequest>(r => r.TestClubId == _newClubId)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequest_WithoutCopyFromClubId_CreatesNewClub()
        {
            // Arrange
            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "New Test Club",
                ClubInitials = "ntc",
                ClubWebsite = "example.com",
                ClubLocation = "Boston, MA",
                ContactEmail = "test@example.com",
                RequestApproved = null
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.SaveNewClub(It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            _coreClubServiceMock.Setup(s => s.UpdateClub(It.IsAny<Club>()))
                .Returns(Task.CompletedTask);

            var scoringSystems = new List<ScoringSystem>
            {
                new ScoringSystem { Id = Guid.NewGuid(), Name = "Series Scoring" }
            };

            _coreScoringServiceMock.Setup(s => s.CreateDefaultScoringSystemsAsync(_newClubId, It.IsAny<string>()))
                .ReturnsAsync(scoringSystems);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessRequest(_requestId, test: false, copyFromClubId: null);

            // Assert
            _coreClubServiceMock.Verify(
                s => s.SaveNewClub(It.Is<Club>(c =>
                    c.Name == "New Test Club"
                    && c.Initials == "NTC"
                    && c.IsHidden == true
                    && c.Url.StartsWith("http")
                    && c.Description == "_Boston, MA_"
                    && c.Locale == "en-US")),
                Times.Once);

            _coreScoringServiceMock.Verify(
                s => s.CreateDefaultScoringSystemsAsync(_newClubId, "NTC"),
                Times.Once);

            _coreClubServiceMock.Verify(
                s => s.UpdateClub(It.Is<Club>(c => c.DefaultScoringSystemId == scoringSystems[0].Id)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequest_WithTestFalse_CreatesClubWithoutTestSuffix()
        {
            // Arrange
            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "Public Club",
                ClubInitials = "pub",
                ClubWebsite = "https://publicclub.com",
                ClubLocation = null,
                ContactEmail = "test@example.com",
                RequestApproved = null
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.SaveNewClub(It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            var scoringSystems = new List<ScoringSystem>
            {
                new ScoringSystem { Id = Guid.NewGuid(), Name = "Series Scoring" }
            };

            _coreScoringServiceMock.Setup(s => s.CreateDefaultScoringSystemsAsync(_newClubId, It.IsAny<string>()))
                .ReturnsAsync(scoringSystems);

            _coreClubServiceMock.Setup(s => s.UpdateClub(It.IsAny<Club>()))
                .Returns(Task.CompletedTask);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessRequest(_requestId, test: false, copyFromClubId: null);

            // Assert
            _coreClubServiceMock.Verify(
                s => s.SaveNewClub(It.Is<Club>(c => c.Initials == "PUB")),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequest_AlwaysSetsTestClubIdForNewClubs()
        {
            // Arrange
            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "Test Club",
                ClubInitials = "tst",
                ClubWebsite = "testclub.com",
                ClubLocation = null,
                ContactEmail = "test@example.com",
                RequestApproved = null
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.SaveNewClub(It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            var scoringSystems = new List<ScoringSystem>
            {
                new ScoringSystem { Id = Guid.NewGuid(), Name = "Series Scoring" }
            };

            _coreScoringServiceMock.Setup(s => s.CreateDefaultScoringSystemsAsync(_newClubId, It.IsAny<string>()))
                .ReturnsAsync(scoringSystems);

            _coreClubServiceMock.Setup(s => s.UpdateClub(It.IsAny<Club>()))
                .Returns(Task.CompletedTask);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessRequest(_requestId, test: false, copyFromClubId: null);

            // Assert
            _coreClubRequestServiceMock.Verify(s => s.UpdateRequest(
                It.Is<ClubRequest>(r => r.TestClubId == _newClubId)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequest_NormalizesInitialsToUppercase()
        {
            // Arrange
            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "Lower Case Club",
                ClubInitials = "lcc",
                ClubWebsite = "example.com",
                ClubLocation = null,
                ContactEmail = "test@example.com",
                RequestApproved = null
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.SaveNewClub(It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            var scoringSystems = new List<ScoringSystem>
            {
                new ScoringSystem { Id = Guid.NewGuid(), Name = "Series Scoring" }
            };

            _coreScoringServiceMock.Setup(s => s.CreateDefaultScoringSystemsAsync(_newClubId, It.IsAny<string>()))
                .ReturnsAsync(scoringSystems);

            _coreClubServiceMock.Setup(s => s.UpdateClub(It.IsAny<Club>()))
                .Returns(Task.CompletedTask);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessRequest(_requestId, test: false, copyFromClubId: null);

            // Assert
            _coreClubServiceMock.Verify(
                s => s.SaveNewClub(It.Is<Club>(c => c.Initials == "LCC")),
                Times.Once);

            _coreScoringServiceMock.Verify(
                s => s.CreateDefaultScoringSystemsAsync(_newClubId, "LCC"),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequest_AddsPermissionWithLowercaseEmail()
        {
            // Arrange
            var email = "Test@Example.COM";
            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "Test Club",
                ClubInitials = "tst",
                ClubWebsite = "example.com",
                ClubLocation = null,
                ContactEmail = email,
                RequestApproved = null
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.SaveNewClub(It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            var scoringSystems = new List<ScoringSystem>
            {
                new ScoringSystem { Id = Guid.NewGuid(), Name = "Series Scoring" }
            };

            _coreScoringServiceMock.Setup(s => s.CreateDefaultScoringSystemsAsync(_newClubId, It.IsAny<string>()))
                .ReturnsAsync(scoringSystems);

            _coreClubServiceMock.Setup(s => s.UpdateClub(It.IsAny<Club>()))
                .Returns(Task.CompletedTask);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessRequest(_requestId, test: false, copyFromClubId: null);

            // Assert
            _coreUserServiceMock.Verify(
                s => s.AddPermission(_newClubId, "test@example.com"),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequest_EnsuresHttpPrefixOnUrl()
        {
            // Arrange
            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "Test Club",
                ClubInitials = "tst",
                ClubWebsite = "example.com",
                ClubLocation = null,
                ContactEmail = "test@example.com",
                RequestApproved = null
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.SaveNewClub(It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            var scoringSystems = new List<ScoringSystem>
            {
                new ScoringSystem { Id = Guid.NewGuid(), Name = "Series Scoring" }
            };

            _coreScoringServiceMock.Setup(s => s.CreateDefaultScoringSystemsAsync(_newClubId, It.IsAny<string>()))
                .ReturnsAsync(scoringSystems);

            _coreClubServiceMock.Setup(s => s.UpdateClub(It.IsAny<Club>()))
                .Returns(Task.CompletedTask);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessRequest(_requestId, test: false, copyFromClubId: null);

            // Assert
            _coreClubServiceMock.Verify(
                s => s.SaveNewClub(It.Is<Club>(c => c.Url.StartsWith("http"))),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequest_ClearsClubMemoryCache()
        {
            // Arrange
            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "Test Club",
                ClubInitials = "tst",
                ClubWebsite = "example.com",
                ClubLocation = null,
                ContactEmail = "test@example.com",
                RequestApproved = null
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.SaveNewClub(It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            var scoringSystems = new List<ScoringSystem>
            {
                new ScoringSystem { Id = Guid.NewGuid(), Name = "Series Scoring" }
            };

            _coreScoringServiceMock.Setup(s => s.CreateDefaultScoringSystemsAsync(_newClubId, It.IsAny<string>()))
                .ReturnsAsync(scoringSystems);

            _coreClubServiceMock.Setup(s => s.UpdateClub(It.IsAny<Club>()))
                .Returns(Task.CompletedTask);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessRequest(_requestId, test: false, copyFromClubId: null);

            // Assert
            _memoryCacheMock.Verify(
                m => m.Remove("CachedClubList"),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequest_SetsRequestApprovedWhenNull()
        {
            // Arrange
            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "Test Club",
                ClubInitials = "tst",
                ClubWebsite = "example.com",
                ClubLocation = null,
                ContactEmail = "test@example.com",
                RequestApproved = null
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.SaveNewClub(It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            var scoringSystems = new List<ScoringSystem>
            {
                new ScoringSystem { Id = Guid.NewGuid(), Name = "Series Scoring" }
            };

            _coreScoringServiceMock.Setup(s => s.CreateDefaultScoringSystemsAsync(_newClubId, It.IsAny<string>()))
                .ReturnsAsync(scoringSystems);

            _coreClubServiceMock.Setup(s => s.UpdateClub(It.IsAny<Club>()))
                .Returns(Task.CompletedTask);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            var beforeTime = DateTime.UtcNow;
            await _service.ProcessRequest(_requestId, test: false, copyFromClubId: null);
            var afterTime = DateTime.UtcNow;

            // Assert
            _coreClubRequestServiceMock.Verify(
                s => s.UpdateRequest(It.Is<ClubRequest>(r =>
                    r.RequestApproved.HasValue
                    && r.RequestApproved.Value >= beforeTime
                    && r.RequestApproved.Value <= afterTime.AddSeconds(1))),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequest_DoesNotUpdateRequestApprovedWhenAlreadySet()
        {
            // Arrange
            var originalApprovedTime = DateTime.UtcNow.AddDays(-1);
            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "Test Club",
                ClubInitials = "tst",
                ClubWebsite = "example.com",
                ClubLocation = null,
                ContactEmail = "test@example.com",
                RequestApproved = originalApprovedTime
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.CopyClubAsync(_copyFromClubId, It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessRequest(_requestId, test: true, copyFromClubId: _copyFromClubId);

            // Assert
            _coreClubRequestServiceMock.Verify(
                s => s.UpdateRequest(It.Is<ClubRequest>(r => r.RequestApproved == originalApprovedTime)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequest_SetsDefaultScoringSystemWhenSystemsCreated()
        {
            // Arrange
            var scoringSystem1 = new ScoringSystem { Id = Guid.NewGuid(), Name = "Series Scoring" };
            var scoringSystem2 = new ScoringSystem { Id = Guid.NewGuid(), Name = "Regatta Scoring" };

            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "Test Club",
                ClubInitials = "tst",
                ClubWebsite = "example.com",
                ClubLocation = null,
                ContactEmail = "test@example.com",
                RequestApproved = null
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.SaveNewClub(It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            var scoringSystems = new List<ScoringSystem> { scoringSystem1, scoringSystem2 };

            _coreScoringServiceMock.Setup(s => s.CreateDefaultScoringSystemsAsync(_newClubId, It.IsAny<string>()))
                .ReturnsAsync(scoringSystems);

            _coreClubServiceMock.Setup(s => s.UpdateClub(It.IsAny<Club>()))
                .Returns(Task.CompletedTask);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessRequest(_requestId, test: false, copyFromClubId: null);

            // Assert
            _coreClubServiceMock.Verify(
                s => s.UpdateClub(It.Is<Club>(c =>
                    c.Id == _newClubId
                    && c.DefaultScoringSystemId == scoringSystem1.Id)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequest_DoesNotUpdateClubWhenNoScoringSystemsCreated()
        {
            // Arrange
            var request = new ClubRequest
            {
                Id = _requestId,
                ClubName = "Test Club",
                ClubInitials = "tst",
                ClubWebsite = "example.com",
                ClubLocation = null,
                ContactEmail = "test@example.com",
                RequestApproved = null
            };

            _coreClubRequestServiceMock.Setup(s => s.GetRequest(_requestId))
                .ReturnsAsync(request);

            _coreClubServiceMock.Setup(s => s.SaveNewClub(It.IsAny<Club>()))
                .ReturnsAsync(_newClubId);

            var scoringSystems = new List<ScoringSystem>();

            _coreScoringServiceMock.Setup(s => s.CreateDefaultScoringSystemsAsync(_newClubId, It.IsAny<string>()))
                .ReturnsAsync(scoringSystems);

            _coreUserServiceMock.Setup(s => s.AddPermission(_newClubId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _coreClubRequestServiceMock.Setup(s => s.UpdateRequest(It.IsAny<ClubRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessRequest(_requestId, test: false, copyFromClubId: null);

            // Assert
            _coreClubServiceMock.Verify(
                s => s.SaveNewClub(It.IsAny<Club>()),
                Times.Once);
            _coreClubServiceMock.Verify(
                s => s.UpdateClub(It.IsAny<Club>()),
                Times.Never);
        }
    }
}
