using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class ClubCollectionViewModel<T> : ClubBaseViewModel
    {
        public string ClubInitials { get; set; }
        public IEnumerable<T> List { get; set; }
    }
}
