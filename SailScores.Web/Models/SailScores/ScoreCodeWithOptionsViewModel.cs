using Microsoft.AspNetCore.Mvc.Rendering;

namespace SailScores.Web.Models.SailScores;

public class ScoreCodeWithOptionsViewModel : ScoreCodeViewModel
{
    public List<SelectListItem> FormulaOptions { get; } = new List<SelectListItem>
    {
        new SelectListItem("COD - Use value of Score Like to find another code to use", "COD"),
        new SelectListItem("FIN+ - Competitors who finished this race + Formula Value", "FIN+"),
        new SelectListItem("SER+ - Competitors in this series + Formula Value", "SER+"),
        new SelectListItem("CTS+ - Competitors who came to start + Formula Value", "CTS+"),
        new SelectListItem("AVE - Average of other results", "AVE"),
        new SelectListItem("AVE P - Average of previous results", "AVE P" ),
        new SelectListItem("AVE ND - Average of all non-discarded races", "AVE ND"),
        new SelectListItem("PLC% - Place + xx% of DNF score (xx = Formula Value)", "PLC%"),
        new SelectListItem("MAN - Allow scorer to enter score manually", "MAN"),
        new SelectListItem("TIE - Tied with previous finisher", "TIE" ),
        new SelectListItem("FIX - Fixed value = Formula Value", "FIX" ),
    };
}