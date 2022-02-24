using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class SeasonDeleteViewModel : Season
{
    public bool IsDeletable { get; set; }
    public string PreventDeleteReason { get; set; }

}