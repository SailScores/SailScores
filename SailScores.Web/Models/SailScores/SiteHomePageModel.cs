using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class SiteHomePageModel
    {
        public ClubSelectorModel ClubSelectorModel { get; set; }

        public RegattaSelectorModel RegattaSelectorModel { get; set; }
        
    }
}
