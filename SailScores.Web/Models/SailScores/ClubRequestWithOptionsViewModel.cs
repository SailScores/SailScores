using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SailScores.Api.Enumerations;

namespace SailScores.Web.Models.SailScores
{
    public class ClubRequestWithOptionsViewModel : ClubRequestViewModel
    {
        public IList<ClubSummaryViewModel> ClubOptions { get; set; }
    }
}
