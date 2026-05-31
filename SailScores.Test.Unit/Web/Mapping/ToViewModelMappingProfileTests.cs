using SailScores.Core.Model;
using SailScores.Test.Unit.Utilities;
using SailScores.Web.Models.SailScores;
using Xunit;

namespace SailScores.Test.Unit.Web.Mapping;

public class ToViewModelMappingProfileTests
{
    [Fact]
    public void ClubToAdminViewModel_MapsEnableAlternativeSailNumbers()
    {
        var mapper = MapperBuilder.GetSailScoresMapper();
        var club = new Club
        {
            EnableAlternativeSailNumbers = true
        };

        var result = mapper.Map<AdminViewModel>(club);

        Assert.True(result.EnableAlternativeSailNumbers);
    }

    [Fact]
    public void AdminViewModelToAdminEditViewModel_MapsEnableAlternativeSailNumbers()
    {
        var mapper = MapperBuilder.GetSailScoresMapper();
        var adminVm = new AdminViewModel
        {
            EnableAlternativeSailNumbers = true
        };

        var result = mapper.Map<AdminEditViewModel>(adminVm);

        Assert.True(result.EnableAlternativeSailNumbers);
    }
}
