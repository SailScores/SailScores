using AutoMapper;
using Moq;
using SailScores.Core.Model;
using SailScores.Core.Services.Interfaces;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class CompetitorServiceTests
{
    private readonly Mock<SailScores.Core.Services.IClubService> _coreClubServiceMock;
    private readonly Mock<SailScores.Core.Services.ICompetitorService> _coreCompetitorServiceMock;
    private readonly Mock<SailScores.Core.Services.IFleetService> _coreFleetServiceMock;
    private readonly Mock<ICompetitorFieldService> _coreCompetitorFieldServiceMock;
    private readonly IMapper _mapper;
    private readonly CompetitorService _service;

    public CompetitorServiceTests()
    {
        _coreClubServiceMock = new Mock<SailScores.Core.Services.IClubService>();
        _coreCompetitorServiceMock = new Mock<SailScores.Core.Services.ICompetitorService>();
        _coreFleetServiceMock = new Mock<SailScores.Core.Services.IFleetService>();
        _coreCompetitorFieldServiceMock = new Mock<ICompetitorFieldService>();
        _mapper = Utilities.MapperBuilder.GetSailScoresMapper();

        _service = new CompetitorService(
            _coreClubServiceMock.Object,
            _coreCompetitorServiceMock.Object,
            _coreFleetServiceMock.Object,
            _coreCompetitorFieldServiceMock.Object,
            _mapper);
    }

    [Fact]
    public async Task SaveAsync_CreateMultiple_WithHighlyVisibleValues_SavesUndatedFieldValues()
    {
        var clubId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var savedValues = new List<CompetitorFieldValue>();

        _coreClubServiceMock
            .Setup(s => s.GetMinimalForSelectedBoatsFleets(clubId))
            .ReturnsAsync(new List<Fleet>());

        _coreCompetitorServiceMock
            .Setup(s => s.SaveAsync(It.IsAny<Competitor>(), It.IsAny<string>()))
            .Callback<Competitor, string>((competitor, _) => competitor.Id = Guid.NewGuid())
            .Returns(Task.CompletedTask);

        _coreCompetitorFieldServiceMock
            .Setup(s => s.SaveValueAsync(It.IsAny<CompetitorFieldValue>()))
            .Callback<CompetitorFieldValue>(value => savedValues.Add(value))
            .ReturnsAsync((CompetitorFieldValue value) => value);

        var vm = new MultipleCompetitorsWithOptionsViewModel
        {
            BoatClassId = Guid.NewGuid(),
            Competitors = new List<CompetitorViewModel>
            {
                new()
                {
                    Name = "Helm One",
                    SailNumber = "100",
                    CustomFieldValues = new List<CompetitorCustomFieldInputViewModel>
                    {
                        new() { FieldDefinitionId = fieldId, Value = "Blue" },
                        new() { FieldDefinitionId = Guid.NewGuid(), Value = "" }
                    }
                }
            }
        };

        await _service.SaveAsync(vm, clubId, "tester");

        var savedValue = Assert.Single(savedValues);
        Assert.Equal(fieldId, savedValue.FieldDefinitionId);
        Assert.Equal("Blue", savedValue.Value);
        Assert.NotEqual(Guid.Empty, savedValue.CompetitorId);
        Assert.Null(savedValue.EffectiveFrom);
        Assert.Null(savedValue.EffectiveTo);
    }

    [Fact]
    public async Task SaveAsync_CreateMultiple_WithEmptyCompetitorRow_DoesNotSaveCustomFieldValues()
    {
        var clubId = Guid.NewGuid();

        _coreClubServiceMock
            .Setup(s => s.GetMinimalForSelectedBoatsFleets(clubId))
            .ReturnsAsync(new List<Fleet>());

        var vm = new MultipleCompetitorsWithOptionsViewModel
        {
            BoatClassId = Guid.NewGuid(),
            Competitors = new List<CompetitorViewModel>
            {
                new()
                {
                    Name = "",
                    SailNumber = "",
                    CustomFieldValues = new List<CompetitorCustomFieldInputViewModel>
                    {
                        new() { FieldDefinitionId = Guid.NewGuid(), Value = "ShouldNotSave" }
                    }
                }
            }
        };

        await _service.SaveAsync(vm, clubId, "tester");

        _coreCompetitorServiceMock.Verify(
            s => s.SaveAsync(It.IsAny<Competitor>(), It.IsAny<string>()),
            Times.Never);
        _coreCompetitorFieldServiceMock.Verify(
            s => s.SaveValueAsync(It.IsAny<CompetitorFieldValue>()),
            Times.Never);
    }
}
