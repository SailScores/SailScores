using System;
using System.Collections.Generic;
using Moq;
using SailScores.Core.Model;
using SailScores.Core.Services.Interfaces;
using SailScores.Database;
using SailScores.Web.Services;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class CustomViewServiceTests
{
    [Fact]
    public void GetCustomFieldValue_WithEffectiveDate_UsesRequestedDate()
    {
        var fieldDefinitionId = Guid.NewGuid();
        var competitor = new Competitor
        {
            CustomFieldValues = new List<CompetitorFieldValue>
            {
                new()
                {
                    FieldDefinitionId = fieldDefinitionId,
                    Value = "Eligible",
                    EffectiveFrom = new DateTime(2024, 6, 1),
                    EffectiveTo = new DateTime(2024, 6, 20)
                }
            }
        };

        var service = new CustomViewService(
            Mock.Of<ISailScoresContext>(),
            Mock.Of<ISeriesResultsTemplateService>());

        var value = service.GetCustomFieldValue(competitor, fieldDefinitionId, new DateTime(2024, 6, 15));

        Assert.Equal("Eligible", value);
    }

    [Fact]
    public void GetCustomFieldValue_WithEffectiveDateOutsideRange_ReturnsNullWhenNoFallbackExists()
    {
        var fieldDefinitionId = Guid.NewGuid();
        var competitor = new Competitor
        {
            CustomFieldValues = new List<CompetitorFieldValue>
            {
                new()
                {
                    FieldDefinitionId = fieldDefinitionId,
                    Value = "Expired",
                    EffectiveFrom = new DateTime(2024, 6, 1),
                    EffectiveTo = new DateTime(2024, 6, 10)
                }
            }
        };

        var service = new CustomViewService(
            Mock.Of<ISailScoresContext>(),
            Mock.Of<ISeriesResultsTemplateService>());

        var value = service.GetCustomFieldValue(competitor, fieldDefinitionId, new DateTime(2024, 6, 15));

        Assert.Null(value);
    }
}
