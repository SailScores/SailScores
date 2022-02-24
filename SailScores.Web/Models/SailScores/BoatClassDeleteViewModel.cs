using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class BoatClassDeleteViewModel : BoatClass
{
    public bool IsDeletable { get; set; }
    public string PreventDeleteReason { get; set; }

}