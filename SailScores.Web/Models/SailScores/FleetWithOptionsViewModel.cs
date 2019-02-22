using Microsoft.AspNetCore.Mvc.Rendering;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class FleetWithOptionsViewModel : Core.Model.Fleet
    {
        public IEnumerable<BoatClass> BoatClassOptions { get; set; }
        public IEnumerable<Guid> BoatClassIds { get; set; }

        public IEnumerable<Competitor> CompetitorOptions { get; set; }
        public IEnumerable<Guid> CompetitorIds { get; set; }
    }
}
