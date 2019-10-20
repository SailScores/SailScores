using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class AdminTipService : IAdminTipService
    {
        public void AddTips(ref AdminViewModel viewModel)
        {
            //todo: fill in dynamically
            viewModel.Tips = new List<AdminTipViewModel>
            {
                new AdminTipViewModel
                {
                    Title = "Create Season",
                    Details = "Before creating a series, a season needs to be created.",
                    Url = "Season/Create"

                }
            };

        }
    }
}
