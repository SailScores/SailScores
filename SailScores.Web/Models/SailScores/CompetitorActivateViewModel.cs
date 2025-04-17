using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class CompetitorActivateViewModel
{
    public Guid CompetitorId { get; set; }

    public bool Active { get; set; }
}