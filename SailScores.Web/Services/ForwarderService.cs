using SailScores.Web.Models;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

/// <summary>
/// Forwarder service should be used when a url fails to find a matching
/// series, regatta, or competitor.  It is responsible for looking in a
/// list of historical urls to see if there is a match. If one is found,
/// it should return an object that will be used by the controller to
/// redirect to the correct url.
/// </summary>
public class ForwarderService : IForwarderService
{
    public ForwarderService(
        )
    {

    }

    public async Task<ForwarderResult> CheckOldUrls(string url)
    {
        //var result = await _coreService.CheckOldUrls(url);
        throw new NotImplementedException();
        //return new ForwarderResult();
    }
}
