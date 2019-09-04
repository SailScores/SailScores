using Microsoft.AspNetCore.Mvc.Rendering;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class FleetCreateViewModel : Core.Model.Fleet
    {
        public IEnumerable<Guid> BoatClassIds { get; set; }
        public Guid? RegattaId { get; set; }
        
    }
}
