using Microsoft.AspNetCore.Http;
using SailScores.Web.Services;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class TelemetryRequestFilterTests
{
    [Theory]
    [InlineData("/wp-login.php")]
    [InlineData("/admin/Default.ASPX")]
    [InlineData("/.git/config")]
    [InlineData("/.env")]
    [InlineData("/cgi-bin/test.cgi")]
    public void ShouldTrace_ScannerPath_ReturnsFalse(string path)
    {
        Assert.False(TelemetryRequestFilter.ShouldTrace(MakeContext(path)));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/LHYC/series/2026/summer")]
    [InlineData("/LHYC/Race/Details/1234")]
    public void ShouldTrace_AppPath_ReturnsTrue(string path)
    {
        Assert.True(TelemetryRequestFilter.ShouldTrace(MakeContext(path)));
    }

    private static HttpContext MakeContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        return context;
    }
}
