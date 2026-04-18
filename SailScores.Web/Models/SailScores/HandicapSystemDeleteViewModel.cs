using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class HandicapSystemDeleteViewModel : HandicapSystem
{
    public bool IsDeletable { get; set; }
    public string PreventDeleteReason { get; set; }
}
