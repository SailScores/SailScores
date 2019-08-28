using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class RegattaSummary
    {

        public Guid Id { get; set; }
        public String Name { get; set; }
        public String UrlName { get; set; }
        public String Description { get; set; }
        public IList<FleetSummary> Fleets { get; set; }
        public Season Season { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
