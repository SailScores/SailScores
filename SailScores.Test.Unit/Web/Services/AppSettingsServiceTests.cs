using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SailScores.Web.Services;
using System.Collections.Generic;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class AppSettingsServiceTests
{
    private static AppSettingsService MakeService(string preferredHost = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["PreferredHost"] = preferredHost })
            .Build();
        return new AppSettingsService(config);
    }

    private static HttpRequest MakeRequest(string path, string query = "", string host = "www.sailscores.com")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString(host);
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);
        return context.Request;
    }

    [Fact]
    public void GetRelativeUri_ClubPage_ReturnsPathAndQuery()
    {
        var result = MakeService().GetRelativeUri(MakeRequest("/LHYC/series/2026/summer", "?view=compact"));

        Assert.Equal("/LHYC/series/2026/summer?view=compact", result);
    }

    [Fact]
    public void GetRelativeUri_DifferentPreferredHost_ReturnsAbsoluteUri()
    {
        var result = MakeService("www.sailscores.com")
            .GetRelativeUri(MakeRequest("/LHYC", host: "sailscores.azurewebsites.net"));

        Assert.Equal("https://www.sailscores.com/LHYC", result);
    }

    [Fact]
    public void GetRelativeUri_LoginPageWithReturnUrl_ReturnsThatReturnUrl()
    {
        var result = MakeService().GetRelativeUri(
            MakeRequest("/Account/Login", "?returnUrl=%2FLHYC%2FCompetitor%2FTC74"));

        Assert.Equal("/LHYC/Competitor/TC74", result);
    }

    [Fact]
    public void GetRelativeUri_LoginPageWithNestedReturnUrl_ReturnsInnermostPage()
    {
        var result = MakeService().GetRelativeUri(
            MakeRequest("/Account/Login", "?returnUrl=%2FAccount%2FRegister%3Freturnurl%3D%252FLHYC"));

        Assert.Equal("/LHYC", result);
    }

    [Fact]
    public void GetRelativeUri_RegisterPageWithoutReturnUrl_ReturnsNull()
    {
        var result = MakeService().GetRelativeUri(MakeRequest("/Account/Register"));

        Assert.Null(result);
    }
}
