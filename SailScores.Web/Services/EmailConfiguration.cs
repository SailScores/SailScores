using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IEmailConfiguration
    {

        string FromAddress { get; set; }

        string SendGridApiKey { get; set; }
    }

    public class EmailConfiguration : IEmailConfiguration
    {
        public string FromAddress { get; set; }
        public string SendGridApiKey { get; set; }


    }
}
