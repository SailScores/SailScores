using SailScores.Web.Models;

namespace SailScores.Web.Services.Interfaces;

public interface IForwarderService
{
    Task<ForwarderResult> CheckOldUrls(string url);
}