using System.Threading.Tasks;

namespace Sailscores.Web.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
