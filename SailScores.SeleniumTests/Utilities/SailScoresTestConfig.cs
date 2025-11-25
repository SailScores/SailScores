using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.SeleniumTests
{
    public class SailScoresTestConfig
    {
        public string BaseUrl { get; set; }
        public string TestEmail { get; set; }
        public string TestPassword { get; set; }
        public string TestClubInitials { get; set; }
        public bool Headless { get; set; } = true;
    }
}
