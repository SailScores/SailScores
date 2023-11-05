using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace SailScores.Web.Models.SailScores;

public class RegattaViewModel : Core.Model.Regatta
{
    private Guid _seasonId;
    public Guid SeasonId
    {
        get
        {
            if (this.Season != null)
            {
                return this.Season.Id;
            }
            return _seasonId;
        }
        set
        {
            _seasonId = value;
        }
    }

    public string LongDescription
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append($"Scores for the {this.Season.Name} {this.Name} Regatta");
            if(this.StartDate
                .HasValue)
            {
                sb.Append($" ({this.StartDate.Value.ToString("d")}");
                if (this.EndDate.HasValue)
                {
                    sb.Append($"-{this.EndDate.Value.ToString("d")}");
                }
                sb.Append(")");
            }

            if(this.Fleets.Sum(f => f.Competitors.Count()) > 0)
            {
                sb.Append($" {this.Fleets.Sum(f => f.Competitors.Count())} competitors");
            }
            
            var fleetCount = this.Fleets.Count();
            if (fleetCount > 0)
            {
                sb.Append(": ");
                sb.Append(this.Fleets.First().NickName?? this.Fleets.First().Name);
                if (fleetCount > 1)
                {
                    for(int i = 1; i < fleetCount - 1; i++)
                    {
                        sb.Append($", {this.Fleets.ElementAt(i).NickName ?? this.Fleets.ElementAt(i).Name}");
                    }
                    sb.Append($" and {this.Fleets.ElementAt(fleetCount-1).NickName ?? this.Fleets.ElementAt(fleetCount-1).Name}");
                }
            }
            return sb.ToString();
        }
    }
}