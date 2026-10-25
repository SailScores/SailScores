using System;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using Xunit;

namespace SailScores.Test.Unit.Web.Models;

public class SeriesResultsTemplateViewModelTests
{
    [Fact]
    public void ToModel_FromTemplate_RoundTripsSettings()
    {
        var template = new SeriesResultsTemplate
        {
            Id = Guid.NewGuid(),
            ClubId = Guid.NewGuid(),
            Name = "Sparse",
            SailNumberVisibility = ColumnVisibility.OnLargerScreens,
            CompetitorNameVisibility = ColumnVisibility.Always,
            CompetitorNameHeader = "Skipper",
            BoatNameVisibility = ColumnVisibility.Hidden,
            BoatNameHeader = "Yacht",
            CompetitorClubVisibility = ColumnVisibility.Always,
            PreDiscardTotalVisibility = ColumnVisibility.OnLargerScreens,
            ParticipationVisibility = ColumnVisibility.Always,
            TotalsSeparator = true,
            ShowClubLogo = true,
            HighlightTopThree = false,
            ColumnSeparators = ResultsColumnSeparator.RaceDays,
            RaceColumnsVisibility = ColumnVisibility.Hidden,
            DiscardStrikethrough = false,
            DiscardFaded = false,
            DiscardColor = ResultsTextColor.Orange,
            ScoreCodes = new System.Collections.Generic.List<SeriesResultsTemplateScoreCode>
            {
                new() { Code = "DNC", Display = ScoreCodeDisplay.Hidden, Faded = true }
            }
        };

        var result = new SeriesResultsTemplateViewModel(template).ToModel();

        Assert.Equivalent(template, result);
    }
}
