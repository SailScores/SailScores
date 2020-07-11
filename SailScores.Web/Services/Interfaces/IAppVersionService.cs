namespace SailScores.Web.Services
{
    public interface IAppVersionService
    {
        string Version { get; }
        string InformationalVersion { get; }
    }
}
