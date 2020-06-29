using SailScores.Core.Model;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores
{
    public class ClubSelectorModel
    {
        public List<Club> Clubs { get; set; }

        public string SelectedClubInitials { get; set; }
    }
}
