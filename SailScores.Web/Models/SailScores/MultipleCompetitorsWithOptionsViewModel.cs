using Microsoft.AspNetCore.Mvc.Rendering;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class MultipleCompetitorsWithOptionsViewModel
    {
        public IOrderedEnumerable<BoatClass> BoatClassOptions { get; set; }

        [Required]
        [Display(Name="Boat Class")]
        public Guid? BoatClassId { get; set; }
        public IList<FleetSummary> FleetOptions { get; set; }

        public IList<Guid> FleetIds { get; set; }

        public IList<CompetitorViewModel> Competitors { get; set; }

    }
}
