using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class RaceViewModel
    {

        public Guid Id { get; set; }

        public Guid ClubId { get; set; }
        public Club Club { get; set; }
        [StringLength(200)]
        public String Name { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:MMMM d, yyyy}")]
        public DateTime? Date { get; set; }

        // Typically the order of the race for a given date, but may not be.
        // used for display order after date. 
        public int Order { get; set; }
        [StringLength(1000)]
        public String Description { get; set; }
        public String TrackingUrl { get; set; }

        public Fleet Fleet { get; set; }

        public IList<Series> Series { get; set; }

        public Season Season { get; set; }

        public RaceState? State { get; set; }

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

        public IList<ScoreViewModel> Scores { get; set; }

    }
}
