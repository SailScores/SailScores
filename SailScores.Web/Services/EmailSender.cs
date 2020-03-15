using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    // This class is used by the application to send email for account confirmation and password reset.
    // For more details see https://go.microsoft.com/fwlink/?LinkID=532713
    public class EmailSender : IEmailSender
    {
        private readonly IEmailConfiguration _emailConfiguration;

        public EmailSender(IEmailConfiguration emailConfiguration)
        {
            _emailConfiguration = emailConfiguration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {

            var apiKey = _emailConfiguration.SendGridApiKey;
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_emailConfiguration.FromAddress);            
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);
            var response = await client.SendEmailAsync(msg);
        }
    }
}