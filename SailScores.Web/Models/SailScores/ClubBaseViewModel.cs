using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public abstract class ClubBaseViewModel
    {
        public string ClubInitials { get; set; }
        public bool CanEdit { get; set; }
    }
}
