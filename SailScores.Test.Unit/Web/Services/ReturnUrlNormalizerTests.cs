using SailScores.Web.Services;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class ReturnUrlNormalizerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Empty_ReturnsNull(string returnUrl)
    {
        Assert.Null(ReturnUrlNormalizer.Normalize(returnUrl));
    }

    [Theory]
    [InlineData("/LHYC/Competitor/TC74")]
    [InlineData("/LHYC/series/2026/summer?view=compact")]
    [InlineData("https://www.sailscores.com/PWA/2026/regatta")]
    [InlineData("/Identity/Account/Manage")]
    [InlineData("/Accounting")]
    public void Normalize_NonAccountUrl_ReturnsUnchanged(string returnUrl)
    {
        Assert.Equal(returnUrl, ReturnUrlNormalizer.Normalize(returnUrl));
    }

    [Theory]
    [InlineData("/Account/Login")]
    [InlineData("/account/register")]
    [InlineData("https://www.sailscores.com/Account/Login")]
    public void Normalize_AccountUrlWithoutReturnUrl_ReturnsNull(string returnUrl)
    {
        Assert.Null(ReturnUrlNormalizer.Normalize(returnUrl));
    }

    [Theory]
    [InlineData("/Account/Register?returnurl=%2FLHYC%2FCompetitor%2FTC74")]
    [InlineData("/Account/Login?returnUrl=%2FAccount%2FRegister%3Freturnurl%3D%252FLHYC%252FCompetitor%252FTC74")]
    [InlineData("https://www.sailscores.com/Account/Login?returnUrl=https%3A%2F%2Fwww.sailscores.com%2FAccount%2FRegister%3FreturnUrl%3D%252FLHYC%252FCompetitor%252FTC74")]
    public void Normalize_NestedAccountUrls_ReturnsInnermostPage(string returnUrl)
    {
        Assert.Equal("/LHYC/Competitor/TC74", ReturnUrlNormalizer.Normalize(returnUrl));
    }

    [Fact]
    public void Normalize_MalformedNestedUrlFromCrawlLoop_ReturnsNull()
    {
        // Shape seen in production: the old stripper dropped the '?' before returnurl
        var returnUrl = "/Account/Login?returnUrl=%2FAccount%2FRegisterreturnurl%3D%252FLHYC%252FCompetitor%252FTC74";

        Assert.Null(ReturnUrlNormalizer.Normalize(returnUrl));
    }

    [Fact]
    public void Normalize_IsIdempotent()
    {
        var once = ReturnUrlNormalizer.Normalize(
            "/Account/Login?returnUrl=%2FAccount%2FRegister%3Freturnurl%3D%252FSSC");

        Assert.Equal(once, ReturnUrlNormalizer.Normalize(once));
    }
}
