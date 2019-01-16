using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class ClubItemViewModel<T> : ClubBaseViewModel
    {
        public T Item { get; set; }
    }
}
