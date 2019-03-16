using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.Sitemap
{
    public class SitemapUrl
    {
        public string Url { get; set; }
        public DateTime? Modified { get; set; }
        public ChangeFrequency? ChangeFrequency { get; set; }
        public double? Priority { get; set; }
    }
}
