using SailScores.Web.Services;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SailScores.Web.Extensions
{
    public static class EmailSenderExtensions
    {
        public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string email, string link)
        {
            return emailSender.SendEmailAsync(email, "Confirm your SailScores account",
                $"<br/>Please confirm your SailScores account by clicking this link: <br/> <a href='{HtmlEncoder.Default.Encode(link)}'>{link}</a><br/>");
        }
    }
}
