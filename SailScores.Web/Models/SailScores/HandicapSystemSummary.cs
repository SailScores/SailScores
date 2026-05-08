using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class HandicapSystemSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public HandicapSystemType SystemType { get; set; }
    public string Description { get; set; }
    public Guid? ParentSystemId { get; set; }
    public string ParentSystemName { get; set; }
}
