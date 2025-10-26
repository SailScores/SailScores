using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SailScores.Web.Models.SailScores;

public class RaceViewModel
{

#pragma warning disable CA2227 // Collection properties should be read only
    public Guid Id { get; set; }

    public Guid ClubId { get; set; }
    [StringLength(200)]
    public String Name { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:D}")]
    public DateTime? Date { get; set; }

    // Typically the order of the race for a given date
    // used for display order after date.
    public int Order { get; set; }
    [StringLength(1000)]
    public String Description { get; set; }
    public String TrackingUrl { get; set; }

    public Fleet Fleet { get; set; }

    public IList<Series> Series { get; set; }

    public RegattaViewModel Regatta { get; set; }

    public Season Season { get; set; }

    public RaceState? State { get; set; }

    public WeatherViewModel Weather { get; set; }

    public DateTime? UpdatedDate { get; set; }
    public String UpdatedBy { get; set; }

    // New timing fields
    public DateTime? StartTime { get; set; }
    public bool TrackTimes { get; set; }

    public string DisplayName
    {
        get
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
                case RaceState.Scheduled:
                    sb.Append("Scheduled for ");
                    break;
                case RaceState.Abandoned:
                    sb.Append("Abandoned. ");
                    break;
            }
            if (Date.HasValue)
            {
                sb.Append(Date.Value.ToString("D", CultureInfo.CurrentCulture));
            }
            if (Order > 0
                && State != RaceState.Scheduled)
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

#pragma warning restore CA2227 // Collection properties should be read only
}