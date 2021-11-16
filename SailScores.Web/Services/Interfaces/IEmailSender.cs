namespace SailScores.Web.Services.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);

    Task<string> GetHtmlFromView<T>(
        string viewName,
        T model);
}