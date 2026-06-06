using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Core.Services;

public static class SeriesResultsTemplateHelper
{
    public static ResolvedTemplate GetResolvedTemplate(SeriesResultsTemplate template)
    {
        if (template == null)
        {
            return GetDefaultTemplate();
        }

        return new ResolvedTemplate
        {
            SailNumberVisibility = template.SailNumberVisibility,
            CompetitorNameVisibility = template.CompetitorNameVisibility,
            CompetitorNameHeader = template.CompetitorNameHeader ?? "Helm",
            BoatNameVisibility = template.BoatNameVisibility,
            BoatNameHeader = template.BoatNameHeader ?? "Boat",
            CompetitorClubVisibility = template.CompetitorClubVisibility
        };
    }

    public static ResolvedTemplate GetDefaultTemplate(bool isRegatta = false)
    {
        return new ResolvedTemplate
        {
            SailNumberVisibility = ColumnVisibility.Always,
            CompetitorNameVisibility = ColumnVisibility.Always,
            CompetitorNameHeader = "Helm",
            BoatNameVisibility = ColumnVisibility.OnLargerScreens,
            BoatNameHeader = "Boat",
            CompetitorClubVisibility = isRegatta ? ColumnVisibility.OnLargerScreens : ColumnVisibility.Hidden
        };
    }
}

public class ResolvedTemplate
{
    public ColumnVisibility SailNumberVisibility { get; set; }
    public ColumnVisibility CompetitorNameVisibility { get; set; }
    public string CompetitorNameHeader { get; set; }
    public ColumnVisibility BoatNameVisibility { get; set; }
    public string BoatNameHeader { get; set; }
    public ColumnVisibility CompetitorClubVisibility { get; set; }
}
