namespace SailScores.Web.Services.Interfaces;

public interface IAppVersionService
{
    string Version { get; }
    string InformationalVersion { get; }
}