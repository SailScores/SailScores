using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class FleetDeleteViewModel : Fleet
{
    public bool IsDeletable { get; set; }
    public string PreventDeleteReason { get; set; }
    public bool IsRegattaFleet { get; internal set; }
}