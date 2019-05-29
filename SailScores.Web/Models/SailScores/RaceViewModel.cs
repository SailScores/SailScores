using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class RaceViewModel : Race
    {

        public string DisplayName { get
            {
                StringBuilder sb = new StringBuilder();

                if (!String.IsNullOrWhiteSpace(Name))
                {
                    sb.Append(Name);
                    sb.Append(" ");
                }
                var useParens = !String.IsNullOrWhiteSpace(Name) && Date.HasValue;

                if (useParens)
                {
                    sb.Append("(");
                }
                switch (State)
                {
                    case Api.Enumerations.RaceState.Scheduled:
                        sb.Append("Scheduled for ");
                        break;
                    case Api.Enumerations.RaceState.Abandoned:
                        sb.Append("Abandoned. ");
                        break;
                }
                if (Date.HasValue)
                {
                    sb.Append(Date.Value.ToString("MMMM d, yyyy"));
                }
                if (Order > 0)
                {
                    sb.Append(" R");
                    sb.Append(Order);
                }
                if (useParens)
                {
                    sb.Append(")");
                }
                return sb.ToString();
            }
        }

    }
}
