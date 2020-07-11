using System;

namespace SailScores.Web.Models.SailScores
{
    public class AboutViewModel
    {
        public String Version { get; set; }
        public String Framework { get; internal set; }
        public string GitHash { get; internal set; }
        public string ShortGitHash { get; internal set; }
        public string BuildId { get; internal set; }
        public string BuildNumber { get; internal set; }
    }
}
