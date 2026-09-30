using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Core.Services;

public static class SeriesResultsTemplateHelper
{
    public static ResolvedTemplate GetResolvedTemplate(SeriesResultsTemplate template, bool isRegatta = false)
    {
        return GetResolvedTemplate(template, isRegatta, fallbackTemplate: null);
    }

    public static ResolvedTemplate GetResolvedTemplate(
        SeriesResultsTemplate template,
        bool isRegatta,
        SeriesResultsTemplate fallbackTemplate)
    {
        var effectiveTemplate = template ?? fallbackTemplate;
        if (effectiveTemplate == null)
        {
            return GetDefaultTemplate(isRegatta);
        }

        return new ResolvedTemplate
        {
            SailNumberVisibility = effectiveTemplate.SailNumberVisibility,
            CompetitorNameVisibility = effectiveTemplate.CompetitorNameVisibility,
            CompetitorNameHeader = effectiveTemplate.CompetitorNameHeader ?? "Helm",
            BoatNameVisibility = effectiveTemplate.BoatNameVisibility,
            BoatNameHeader = effectiveTemplate.BoatNameHeader ?? "Boat",
            CompetitorClubVisibility = effectiveTemplate.CompetitorClubVisibility,
            ShowPreDiscardTotal = effectiveTemplate.ShowPreDiscardTotal,
            ShowClubLogo = effectiveTemplate.ShowClubLogo,
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
            CompetitorClubVisibility = isRegatta ? ColumnVisibility.OnLargerScreens : ColumnVisibility.Hidden,
            ShowPreDiscardTotal = false,
            ShowClubLogo = false,
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
    public bool ShowPreDiscardTotal { get; set; }
    public bool ShowClubLogo { get; set; }
}
