namespace SailScores.Web.Services.Interfaces;

public interface ISpeechService
{
    Task<string> GetToken();
    string GetRegion();
}