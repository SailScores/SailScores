using AutoMapper;
using Moq;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class FleetServiceTests
{
    private readonly Mock<SailScores.Core.Services.IClubService> _coreClubServiceMock;
    private readonly Mock<SailScores.Core.Services.IFleetService> _coreFleetServiceMock;
    private readonly Mock<SailScores.Core.Services.ICompetitorService> _coreCompetitorServiceMock;
    private readonly Mock<IRegattaService> _regattaServiceMock;
    private readonly IMapper _mapper;
    private readonly FleetService _service;

    private readonly Guid _clubId = Guid.NewGuid();
    private const string ClubInitials = "TEST";

    public FleetServiceTests()
    {
        _coreClubServiceMock = new Mock<SailScores.Core.Services.IClubService>();
        _coreFleetServiceMock = new Mock<SailScores.Core.Services.IFleetService>();
        _coreCompetitorServiceMock = new Mock<SailScores.Core.Services.ICompetitorService>();
        _regattaServiceMock = new Mock<IRegattaService>();
        _mapper = Utilities.MapperBuilder.GetSailScoresMapper();

        _coreClubServiceMock.Setup(s => s.GetClubId(ClubInitials)).ReturnsAsync(_clubId);
        _coreClubServiceMock.Setup(s => s.GetMinimalForSelectedBoatsFleets(_clubId)).ReturnsAsync(new List<Fleet>());
        _coreClubServiceMock.Setup(s => s.GetAllBoatClasses(_clubId)).ReturnsAsync(new List<BoatClass>());
        _coreCompetitorServiceMock.Setup(s => s.GetCompetitorsAsync(_clubId, null, true)).ReturnsAsync(new List<Competitor>());
        _coreFleetServiceMock.Setup(s => s.GetCompetitorFleetMembership(_clubId)).ReturnsAsync(new Dictionary<Guid, IList<Guid>>());
        _coreFleetServiceMock.Setup(s => s.GetClubRegattaFleets(_clubId)).ReturnsAsync(new Dictionary<Guid, IEnumerable<Guid>>());
        _regattaServiceMock.Setup(s => s.GetAllRegattaSummaryAsync(ClubInitials)).ReturnsAsync(new List<RegattaSummaryViewModel>());

        _service = new FleetService(
            _coreClubServiceMock.Object,
            _coreFleetServiceMock.Object,
            _coreCompetitorServiceMock.Object,
            _regattaServiceMock.Object,
            _mapper);
    }

    [Fact]
    public async Task GetFleetManagementViewModel_Always_OnlyIncludesSelectedBoatsFleetsAsColumns()
    {
        var selectedBoatsFleet = new Fleet { Id = Guid.NewGuid(), Name = "Selected Boats Fleet", ClubId = _clubId };
        _coreClubServiceMock
            .Setup(s => s.GetMinimalForSelectedBoatsFleets(_clubId))
            .ReturnsAsync(new List<Fleet> { selectedBoatsFleet });
        _coreCompetitorServiceMock
            .Setup(s => s.GetCompetitorsAsync(_clubId, null, true))
            .ReturnsAsync(new List<Competitor>());
        _coreFleetServiceMock
            .Setup(s => s.GetCompetitorFleetMembership(_clubId))
            .ReturnsAsync(new Dictionary<Guid, IList<Guid>>());

        var result = await _service.GetFleetManagementViewModel(ClubInitials);

        var fleet = Assert.Single(result.Fleets);
        Assert.Equal(selectedBoatsFleet.Id, fleet.Id);
    }

    [Fact]
    public async Task GetFleetManagementViewModel_CompetitorInFleet_MarksFleetMembershipTrue()
    {
        var fleet = new Fleet { Id = Guid.NewGuid(), Name = "Fleet", ClubId = _clubId };
        var competitor = new Competitor { Id = Guid.NewGuid(), Name = "Comp1", ClubId = _clubId };

        _coreClubServiceMock
            .Setup(s => s.GetMinimalForSelectedBoatsFleets(_clubId))
            .ReturnsAsync(new List<Fleet> { fleet });
        _coreCompetitorServiceMock
            .Setup(s => s.GetCompetitorsAsync(_clubId, null, true))
            .ReturnsAsync(new List<Competitor> { competitor });
        _coreFleetServiceMock
            .Setup(s => s.GetCompetitorFleetMembership(_clubId))
            .ReturnsAsync(new Dictionary<Guid, IList<Guid>>
            {
                { competitor.Id, new List<Guid> { fleet.Id } }
            });

        var result = await _service.GetFleetManagementViewModel(ClubInitials);

        var row = Assert.Single(result.Competitors);
        Assert.True(row.FleetMembership[fleet.Id]);
    }

    [Fact]
    public async Task GetFleetManagementViewModel_CompetitorNotInFleet_MarksFleetMembershipFalse()
    {
        var fleet = new Fleet { Id = Guid.NewGuid(), Name = "Fleet", ClubId = _clubId };
        var competitor = new Competitor { Id = Guid.NewGuid(), Name = "Comp1", ClubId = _clubId };

        _coreClubServiceMock
            .Setup(s => s.GetMinimalForSelectedBoatsFleets(_clubId))
            .ReturnsAsync(new List<Fleet> { fleet });
        _coreCompetitorServiceMock
            .Setup(s => s.GetCompetitorsAsync(_clubId, null, true))
            .ReturnsAsync(new List<Competitor> { competitor });
        _coreFleetServiceMock
            .Setup(s => s.GetCompetitorFleetMembership(_clubId))
            .ReturnsAsync(new Dictionary<Guid, IList<Guid>>());

        var result = await _service.GetFleetManagementViewModel(ClubInitials);

        var row = Assert.Single(result.Competitors);
        Assert.False(row.FleetMembership[fleet.Id]);
    }

    [Fact]
    public async Task GetFleetManagementViewModel_Always_IncludesBoatClassesOrderedByName()
    {
        _coreClubServiceMock
            .Setup(s => s.GetAllBoatClasses(_clubId))
            .ReturnsAsync(new List<BoatClass>
            {
                new() { Id = Guid.NewGuid(), Name = "Zulu" },
                new() { Id = Guid.NewGuid(), Name = "Alpha" }
            });

        var result = await _service.GetFleetManagementViewModel(ClubInitials);

        Assert.Equal(2, result.BoatClasses.Count);
        Assert.Equal("Alpha", result.BoatClasses[0].Name);
        Assert.Equal("Zulu", result.BoatClasses[1].Name);
    }

    [Fact]
    public async Task GetFleetManagementViewModel_Always_IncludesRegattasFromRegattaService()
    {
        var regatta = new RegattaSummaryViewModel { Id = Guid.NewGuid(), Name = "Fall Regatta" };
        _regattaServiceMock
            .Setup(s => s.GetAllRegattaSummaryAsync(ClubInitials))
            .ReturnsAsync(new List<RegattaSummaryViewModel> { regatta });

        var result = await _service.GetFleetManagementViewModel(ClubInitials);

        var returned = Assert.Single(result.Regattas);
        Assert.Equal(regatta.Id, returned.Id);
    }

    [Fact]
    public async Task GetFleetManagementViewModel_FleetInRegatta_PopulatesFleetColumnRegattaIds()
    {
        var fleet = new Fleet { Id = Guid.NewGuid(), Name = "Fleet", ClubId = _clubId };
        var regattaId = Guid.NewGuid();
        _coreClubServiceMock
            .Setup(s => s.GetMinimalForSelectedBoatsFleets(_clubId))
            .ReturnsAsync(new List<Fleet> { fleet });
        _coreFleetServiceMock
            .Setup(s => s.GetClubRegattaFleets(_clubId))
            .ReturnsAsync(new Dictionary<Guid, IEnumerable<Guid>>
            {
                { fleet.Id, new List<Guid> { regattaId } }
            });

        var result = await _service.GetFleetManagementViewModel(ClubInitials);

        var column = Assert.Single(result.Fleets);
        Assert.Contains(regattaId, column.RegattaIds);
    }

    [Fact]
    public async Task GetFleetManagementViewModel_FleetActive_PopulatesFleetColumnIsActive()
    {
        var fleet = new Fleet { Id = Guid.NewGuid(), Name = "Fleet", ClubId = _clubId, IsActive = true };
        _coreClubServiceMock
            .Setup(s => s.GetMinimalForSelectedBoatsFleets(_clubId))
            .ReturnsAsync(new List<Fleet> { fleet });

        var result = await _service.GetFleetManagementViewModel(ClubInitials);

        var column = Assert.Single(result.Fleets);
        Assert.True(column.IsActive);
    }
}
