namespace SailScores.Web.Services;

public interface IEmailConfiguration
{

    string FromAddress { get; set; }
    string FromName { get; set; }

    string SendGridApiKey { get; set; }
}

public class EmailConfiguration : IEmailConfiguration
{
    public string FromAddress { get; set; }
    public string FromName { get; set; }
    public string SendGridApiKey { get; set; }


}