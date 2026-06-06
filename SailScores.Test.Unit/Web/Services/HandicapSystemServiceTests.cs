using AutoMapper;
using Moq;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class HandicapSystemServiceTests
{
    private readonly Mock<SailScores.Core.Services.IHandicapService> _coreHandicapServiceMock;
    private readonly IMapper _mapper;
    private readonly HandicapSystemService _service;

    public HandicapSystemServiceTests()
    {
        _coreHandicapServiceMock = new Mock<SailScores.Core.Services.IHandicapService>();
        _mapper = Utilities.MapperBuilder.GetSailScoresMapper();
        _service = new HandicapSystemService(_coreHandicapServiceMock.Object, _mapper);
    }

    [Fact]
    public async Task GetBaseSystemsAsync_ReturnsMappedAndSorted()
    {
        _coreHandicapServiceMock
            .Setup(s => s.GetBaseHandicapSystemsAsync())
            .ReturnsAsync(new List<HandicapSystem>
            {
                new() { Id = Guid.NewGuid(), Name = "Zulu" },
                new() { Id = Guid.NewGuid(), Name = "Alpha" }
            });

        var result = await _service.GetBaseSystemsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Zulu", result[1].Name);
    }

    [Fact]
    public async Task GetClubSystemsAsync_ResolvesParentSystemName()
    {
        var clubId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        _coreHandicapServiceMock
            .Setup(s => s.GetHandicapSystemsAsync(clubId))
            .ReturnsAsync(new List<HandicapSystem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ClubId = clubId,
                    Name = "Club Portsmouth",
                    ParentSystemId = parentId,
                    SystemType = HandicapSystemType.Portsmouth
                }
            });

        _coreHandicapServiceMock
            .Setup(s => s.GetBaseHandicapSystemsAsync())
            .ReturnsAsync(new List<HandicapSystem>
            {
                new()
                {
                    Id = parentId,
                    Name = "Base Portsmouth",
                    SystemType = HandicapSystemType.Portsmouth
                }
            });

        var result = await _service.GetClubSystemsAsync(clubId);

        var single = Assert.Single(result);
        Assert.Equal("Base Portsmouth", single.ParentSystemName);
    }

    [Fact]
    public async Task CreateClubSystemAsync_ForwardsToCoreService_AndReturnsNewId()
    {
        var clubId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var createdId = Guid.NewGuid();

        var model = new CreateHandicapSystemViewModel
        {
            ClubId = clubId,
            ParentSystemId = parentId,
            Name = "Club ToT",
            Description = "desc"
        };

        _coreHandicapServiceMock
            .Setup(s => s.CreateClubHandicapSystemAsync(clubId, parentId, model.Name, model.Description))
            .ReturnsAsync(new HandicapSystem { Id = createdId });

        var result = await _service.CreateClubSystemAsync(model);

        Assert.Equal(createdId, result);
        _coreHandicapServiceMock.Verify(
            s => s.CreateClubHandicapSystemAsync(clubId, parentId, model.Name, model.Description),
            Times.Once);
    }
}
