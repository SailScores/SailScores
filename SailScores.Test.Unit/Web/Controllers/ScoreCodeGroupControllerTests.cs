using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Web.Controllers;
using SailScores.Web.Authorization;
using SailScores.Web.Models.SailScores;
using Xunit;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Test.Unit.Web.Controllers;

public class ScoreCodeGroupControllerTests
{
    private readonly Mock<IClubService> _clubServiceMock;
    private readonly Mock<IScoringService> _scoringServiceMock;
    private readonly Mock<IAuthorizationService> _authServiceMock;
    private readonly ScoreCodeGroupController _controller;

    public ScoreCodeGroupControllerTests()
    {
        _clubServiceMock = new Mock<IClubService>();
        _scoringServiceMock = new Mock<IScoringService>();
        _authServiceMock = new Mock<IAuthorizationService>();
        _controller = new ScoreCodeGroupController(
            _clubServiceMock.Object,
            _scoringServiceMock.Object,
            _authServiceMock.Object);
    }

    [Fact]
    public async Task Create_WhenOverageCodeIsInIncludedCodes_AddModelError()
    {
        // Arrange
        var clubId = Guid.NewGuid();
        var scoringSystemId = Guid.NewGuid();
        var clubInitials = "TST";

        _clubServiceMock
            .Setup(s => s.GetClubId(clubInitials))
            .ReturnsAsync(clubId);

        var scoringSystem = new ScoringSystem
        {
            Id = scoringSystemId,
            ClubId = clubId,
            Name = "Test System",
            ScoreCodes = new List<ScoreCode>(),
            InheritedScoreCodes = new List<ScoreCode>(),
            ScoreCodeGroups = new List<ScoreCodeGroup>(),
            InheritedScoreCodeGroups = new List<ScoreCodeGroup>()
        };

        _scoringServiceMock
            .Setup(s => s.GetScoringSystemAsync(scoringSystemId))
            .ReturnsAsync(scoringSystem);

        var model = new ScoreCodeGroupViewModel
        {
            ScoringSystemId = scoringSystemId,
            Name = "Test Group",
            IncludedCodeNames = new List<string> { "DNC", "OCS" },
            OverageCodeName = "DNC"
        };

        // Act
        var result = await _controller.Create(clubInitials, model);

        // Assert
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains(_controller.ModelState["OverageCodeName"].Errors,
            e => e.ErrorMessage.Contains("cannot be one of the included codes"));
    }

    [Fact]
    public async Task Create_WhenOverageCodeUsedInAnotherGroup_AddModelError()
    {
        // Arrange
        var clubId = Guid.NewGuid();
        var scoringSystemId = Guid.NewGuid();
        var clubInitials = "TST";

        _clubServiceMock
            .Setup(s => s.GetClubId(clubInitials))
            .ReturnsAsync(clubId);

        var existingGroup = new ScoreCodeGroup
        {
            Id = Guid.NewGuid(),
            ScoringSystemId = scoringSystemId,
            OverageCodeName = "DNC",
            IncludedCodeNames = new List<string> { "DNS" }
        };

        var scoringSystem = new ScoringSystem
        {
            Id = scoringSystemId,
            ClubId = clubId,
            Name = "Test System",
            ScoreCodes = new List<ScoreCode>(),
            InheritedScoreCodes = new List<ScoreCode>(),
            ScoreCodeGroups = new List<ScoreCodeGroup> { existingGroup },
            InheritedScoreCodeGroups = new List<ScoreCodeGroup>()
        };

        _scoringServiceMock
            .Setup(s => s.GetScoringSystemAsync(scoringSystemId))
            .ReturnsAsync(scoringSystem);

        var model = new ScoreCodeGroupViewModel
        {
            ScoringSystemId = scoringSystemId,
            Name = "Test Group",
            IncludedCodeNames = new List<string> { "OCS" },
            OverageCodeName = "DNC"
        };

        // Act
        var result = await _controller.Create(clubInitials, model);

        // Assert
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains(_controller.ModelState["OverageCodeName"].Errors,
            e => e.ErrorMessage.Contains("already used as the overage code"));
    }

    [Fact]
    public async Task Create_WhenIncludedCodeIsUsedAsOverageInAnotherGroup_AddModelError()
    {
        // Arrange
        var clubId = Guid.NewGuid();
        var scoringSystemId = Guid.NewGuid();
        var clubInitials = "TST";

        _clubServiceMock
            .Setup(s => s.GetClubId(clubInitials))
            .ReturnsAsync(clubId);

        var existingGroup = new ScoreCodeGroup
        {
            Id = Guid.NewGuid(),
            ScoringSystemId = scoringSystemId,
            OverageCodeName = "OCS",
            IncludedCodeNames = new List<string> { "DNS" }
        };

        var scoringSystem = new ScoringSystem
        {
            Id = scoringSystemId,
            ClubId = clubId,
            Name = "Test System",
            ScoreCodes = new List<ScoreCode>(),
            InheritedScoreCodes = new List<ScoreCode>(),
            ScoreCodeGroups = new List<ScoreCodeGroup> { existingGroup },
            InheritedScoreCodeGroups = new List<ScoreCodeGroup>()
        };

        _scoringServiceMock
            .Setup(s => s.GetScoringSystemAsync(scoringSystemId))
            .ReturnsAsync(scoringSystem);

        var model = new ScoreCodeGroupViewModel
        {
            ScoringSystemId = scoringSystemId,
            Name = "Test Group",
            IncludedCodeNames = new List<string> { "OCS", "DNC" },
            OverageCodeName = "DNS"
        };

        // Act
        var result = await _controller.Create(clubInitials, model);

        // Assert
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains(_controller.ModelState["IncludedCodeNames"].Errors,
            e => e.ErrorMessage.Contains("already used as overage codes"));
    }

    [Fact]
    public async Task Create_WhenInheritedGroupHasConflictingOverageCode_AddModelError()
    {
        // Arrange
        var clubId = Guid.NewGuid();
        var scoringSystemId = Guid.NewGuid();
        var clubInitials = "TST";

        _clubServiceMock
            .Setup(s => s.GetClubId(clubInitials))
            .ReturnsAsync(clubId);

        var inheritedGroup = new ScoreCodeGroup
        {
            Id = Guid.NewGuid(),
            ScoringSystemId = Guid.NewGuid(), // Different system (parent)
            OverageCodeName = "DNC",
            IncludedCodeNames = new List<string> { "DNS" }
        };

        var scoringSystem = new ScoringSystem
        {
            Id = scoringSystemId,
            ClubId = clubId,
            Name = "Test System",
            ScoreCodes = new List<ScoreCode>(),
            InheritedScoreCodes = new List<ScoreCode>(),
            ScoreCodeGroups = new List<ScoreCodeGroup>(),
            InheritedScoreCodeGroups = new List<ScoreCodeGroup> { inheritedGroup }
        };

        _scoringServiceMock
            .Setup(s => s.GetScoringSystemAsync(scoringSystemId))
            .ReturnsAsync(scoringSystem);

        var model = new ScoreCodeGroupViewModel
        {
            ScoringSystemId = scoringSystemId,
            Name = "Test Group",
            IncludedCodeNames = new List<string> { "OCS" },
            OverageCodeName = "DNC"
        };

        // Act
        var result = await _controller.Create(clubInitials, model);

        // Assert
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains(_controller.ModelState["OverageCodeName"].Errors,
            e => e.ErrorMessage.Contains("already used as the overage code"));
    }

    [Fact]
    public async Task Create_WhenAllValidationsPassed_SuccessfullySaves()
    {
        // Arrange
        var clubId = Guid.NewGuid();
        var scoringSystemId = Guid.NewGuid();
        var clubInitials = "TST";

        _clubServiceMock
            .Setup(s => s.GetClubId(clubInitials))
            .ReturnsAsync(clubId);

        var scoringSystem = new ScoringSystem
        {
            Id = scoringSystemId,
            ClubId = clubId,
            Name = "Test System",
            ScoreCodeGroups = new List<ScoreCodeGroup>(),
            InheritedScoreCodeGroups = new List<ScoreCodeGroup>()
        };

        _scoringServiceMock
            .Setup(s => s.GetScoringSystemAsync(scoringSystemId))
            .ReturnsAsync(scoringSystem);

        _scoringServiceMock
            .Setup(s => s.SaveScoreCodeGroupAsync(It.IsAny<ScoreCodeGroup>()))
            .Returns(Task.CompletedTask);

        var model = new ScoreCodeGroupViewModel
        {
            ScoringSystemId = scoringSystemId,
            Name = "Test Group",
            IncludedCodeNames = new List<string> { "DNS", "OCS" },
            OverageCodeName = "DNC"
        };

        // Act
        var result = await _controller.Create(clubInitials, model);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains(clubInitials, redirectResult.Url);
        _scoringServiceMock.Verify(s => s.SaveScoreCodeGroupAsync(It.IsAny<ScoreCodeGroup>()), Times.Once);
    }

    [Fact]
    public async Task Edit_WhenGroupIdChanged_ExcludesCurrentGroupFromValidation()
    {
        // Arrange
        var clubId = Guid.NewGuid();
        var scoringSystemId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var clubInitials = "TST";

        _clubServiceMock
            .Setup(s => s.GetClubId(clubInitials))
            .ReturnsAsync(clubId);

        var scoringSystem = new ScoringSystem
        {
            Id = scoringSystemId,
            ClubId = clubId,
            Name = "Test System",
            ScoreCodeGroups = new List<ScoreCodeGroup>(),
            InheritedScoreCodeGroups = new List<ScoreCodeGroup>()
        };

        _scoringServiceMock
            .Setup(s => s.GetScoringSystemAsync(scoringSystemId))
            .ReturnsAsync(scoringSystem);

        _scoringServiceMock
            .Setup(s => s.SaveScoreCodeGroupAsync(It.IsAny<ScoreCodeGroup>()))
            .Returns(Task.CompletedTask);

        var model = new ScoreCodeGroupViewModel
        {
            Id = groupId,
            ScoringSystemId = scoringSystemId,
            Name = "Test Group",
            IncludedCodeNames = new List<string> { "DNS" },
            OverageCodeName = "DNC"
        };

        // Act
        var result = await _controller.Edit(clubInitials, model);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains(clubInitials, redirectResult.Url);
        _scoringServiceMock.Verify(s => s.SaveScoreCodeGroupAsync(It.IsAny<ScoreCodeGroup>()), Times.Once);
    }
}
