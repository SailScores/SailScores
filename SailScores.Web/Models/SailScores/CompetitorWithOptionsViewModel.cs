using Microsoft.AspNetCore.Mvc.Rendering;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class CompetitorWithOptionsViewModel : Core.Model.Competitor
    {
        public IOrderedEnumerable<BoatClass> BoatClassOptions { get; set; }
    }
}
