using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sailscores.Web.Models.Sailscores
{
    public class ClubItemViewModel<T> : ClubBaseViewModel
    {
        public T Item { get; set; }
    }
}
