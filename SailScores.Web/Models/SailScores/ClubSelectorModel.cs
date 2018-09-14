using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class ClubSelectorModel
    {
        public List<Club> Clubs { get; set; }

        public string SelectedClubInitials { get; set; }
    }
}
