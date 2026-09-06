using SailScores.Core.Utility;
using Xunit;

namespace SailScores.Test.Unit.Core.Utility;

public class ClubDateFormatUtilityTests
{
    [Theory]
    [InlineData("dd/MM/yyyy")]
    [InlineData("MM-dd-yy")]
    [InlineData("d MMM yyyy")]
    public void TryNormalize_WithSupportedFormat_ReturnsNormalizedFormat(string format)
    {
        var success = ClubDateFormatUtility.TryNormalize(format, out var normalized, out var errorMessage);

        Assert.True(success);
        Assert.Equal(format, normalized);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void TryNormalize_WithUpperCaseYearAndDayTokens_NormalizesToLowerCase()
    {
        var success = ClubDateFormatUtility.TryNormalize("DD/MM/YYYY", out var normalized, out var errorMessage);

        Assert.True(success);
        Assert.Equal("dd/MM/yyyy", normalized);
        Assert.Null(errorMessage);
    }

    [Theory]
    [InlineData("hh:mm")]
    [InlineData("dddd, MMM d")]
    [InlineData("MM/dd/yyyy!")]
    public void TryNormalize_WithUnsupportedFormat_ReturnsFalse(string format)
    {
        var success = ClubDateFormatUtility.TryNormalize(format, out var normalized, out var errorMessage);

        Assert.False(success);
        Assert.Null(normalized);
        Assert.False(string.IsNullOrWhiteSpace(errorMessage));
    }
}
