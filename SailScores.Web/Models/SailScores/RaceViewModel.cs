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
