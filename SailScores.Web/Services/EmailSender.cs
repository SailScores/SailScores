using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services
{
    // This class is used by the application to send email for account confirmation and password reset.
    // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
    public class EmailSender : IEmailSender
    {
        private readonly IEmailConfiguration _emailConfiguration;
        private readonly ITemplateHelper _templateHelper;

        public EmailSender(
            IEmailConfiguration emailConfiguration,
            ITemplateHelper templateHelper)
        {
            _emailConfiguration = emailConfiguration;
            _templateHelper = templateHelper;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {

            var apiKey = _emailConfiguration.SendGridApiKey;
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_emailConfiguration.FromAddress, _emailConfiguration.FromName);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);
            await client.SendEmailAsync(msg);
        }

        public async Task<string> GetHtmlFromView<T>(
            string viewName,
            T model)
        {
            return await _templateHelper.GetTemplateHtmlAsStringAsync(viewName, model);
        }
    }
}