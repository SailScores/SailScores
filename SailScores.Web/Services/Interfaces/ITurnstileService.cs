using System.Net;
using System.Threading;

namespace SailScores.Web.Services.Interfaces;

public interface ITurnstileService
{
    bool IsEnabled { get; }
    string SiteKey { get; }

    Task<bool> VerifyAsync(string token, IPAddress remoteIpAddress, CancellationToken cancellationToken = default);
}
