namespace SailScores.Api.Services
{
    public interface ISettings
    {
        string ServerUrl { get; set; }

        string UserName { get; set; }
        string Password { get; set; }

    }
}
